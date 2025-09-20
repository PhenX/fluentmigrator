#region License
//
// Copyright (c) 2007-2024, Fluent Migrator Project
// Copyright (c) 2010, Nathan Brown
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;
using FluentMigrator.Runner.Generators.SQLite;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.SQLite
{
    /// <summary>
    /// The SQLite processor for FluentMigrator.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class SQLiteProcessor : GenericProcessorBase
    {
        [CanBeNull]
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.SQLite;

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "select 1 from {0}sqlite_master where name={1} and type='table'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "select 1 from {0}sqlite_master AS t, {0}pragma_table_info(t.name) AS c where t.type = 'table' AND t.name = {1} AND c.name = {2}";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "select 1 from {0}sqlite_master where name={2} and tbl_name={1} and type='index' and sql LIKE 'CREATE UNIQUE INDEX %'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "select 1 from {0}sqlite_master where name={2} and tbl_name={1} and type='index'";

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        /// <inheritdoc />
        public SQLiteProcessor(
            [NotNull] SQLiteDbFactory factory,
            [NotNull] SQLiteGenerator generator,
            [NotNull] ILogger<SQLiteProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] IServiceProvider serviceProvider,
            [NotNull] SQLiteQuoter quoter)
            : base(() => factory.Factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return !string.IsNullOrWhiteSpace(schemaName) ? Quoter.QuoteValue(schemaName) + "." : string.Empty;
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return Quoter.QuoteValue(name);
        }

        /// <inheritdoc />
        public override bool SchemaExists(string schemaName)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            using (var reader = command.ExecuteReader())
            {
                try
                {
                    if (!reader.Read()) return false;
                    if (int.Parse(reader[0].ToString()) <= 0) return false;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return false;
        }

        /// <inheritdoc />
        protected override void Process(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return;

            if (Options.PreviewOnly)
            {
                ExecuteBatchNonQuery(
                    sql,
                    (sqlBatch) =>
                    {
                        Logger.LogSql(sqlBatch);
                    },
                    (sqlBatch, goCount) =>
                    {
                        Logger.LogSql(sqlBatch);
                        Logger.LogSql($"GO {goCount}");
                    });
                return;
            }

            Logger.LogSql(sql);

            EnsureConnectionIsOpen();

            if (ContainsGo(sql))
            {
                ExecuteBatchNonQuery(
                    sql,
                    (sqlBatch) =>
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            command.ExecuteNonQuery();
                        }
                    },
                    (sqlBatch, goCount) =>
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            for (var i = 0; i != goCount; ++i)
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    });
            }
            else
            {
                ExecuteNonQuery(sql);
            }
        }

        private bool ContainsGo(string sql)
        {
            var containsGo = false;
            var parser = _serviceProvider?.GetService<SQLiteBatchParser>() ?? new SQLiteBatchParser();
            parser.SpecialToken += (sender, args) => containsGo = true;
            using (var source = new TextReaderSource(new StringReader(sql), true))
            {
                parser.Process(source);
            }

            return containsGo;
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = CreateCommand(sql))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (DbException ex)
                {
                    throw new Exception(ex.Message + Environment.NewLine + "While Processing:" + Environment.NewLine + "\"" + command.CommandText + "\"", ex);
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql, Action<string> executeBatch, Action<string, int> executeGo)
        {
            string sqlBatch = string.Empty;

            try
            {
                var parser = _serviceProvider?.GetService<SQLiteBatchParser>() ?? new SQLiteBatchParser();
                parser.SqlText += (sender, args) => { sqlBatch = args.SqlText.Trim(); };
                parser.SpecialToken += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(sqlBatch))
                        return;

                    if (args.Opaque is GoSearcher.GoSearcherParameters goParams)
                    {
                        executeGo(sqlBatch, goParams.Count);
                    }

                    sqlBatch = null;
                };

                using (var source = new TextReaderSource(new StringReader(sql), true))
                {
                    parser.Process(source, stripComments: Options.StripComments);
                }

                if (!string.IsNullOrEmpty(sqlBatch))
                {
                    executeBatch(sqlBatch);
                }
            }
            catch (DbException ex)
            {
                throw new Exception(ex.Message + Environment.NewLine + "While Processing:" + Environment.NewLine + "\"" + sqlBatch + "\"", ex);
            }
        }
    }
}
