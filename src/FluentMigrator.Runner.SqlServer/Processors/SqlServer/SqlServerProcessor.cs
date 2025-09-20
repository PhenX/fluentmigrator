#region License
// Copyright (c) 2018, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

using FluentMigrator.Expressions;
using FluentMigrator.Generation;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.SqlServer
{
    /// <summary>
    /// The SQL Server processor for FluentMigrator.
    /// </summary>
    public class SqlServerProcessor : GenericProcessorBase
    {
        [CanBeNull]
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        protected override string SchemaExistsQuery =>
            "SELECT * FROM sys.schemas WHERE NAME = '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_CATALOG = DB_NAME() AND TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}' AND CONSTRAINT_NAME = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT * FROM sys.indexes WHERE name = '{2}' and object_id = OBJECT_ID('{0}.{1}')";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.SEQUENCES WHERE SEQUENCE_SCHEMA = '{0}' AND SEQUENCE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}' AND COLUMN_DEFAULT LIKE '{3}'";

        /// <inheritdoc />
        public override string DatabaseType { get;}

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerProcessor"/> class.
        /// </summary>
        /// <param name="databaseTypes">The database type names.</param>
        /// <param name="generator">The migration generator.</param>
        /// <param name="quoter">The SQL quoter.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The processor options.</param>
        /// <param name="connectionStringAccessor">The connection string accessor.</param>
        /// <param name="serviceProvider">The service provider.</param>
        protected SqlServerProcessor(
            [NotNull, ItemNotNull] IEnumerable<string> databaseTypes,
            [NotNull] IMigrationGenerator generator,
            [NotNull] IQuoter quoter,
            [NotNull] ILogger logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] IServiceProvider serviceProvider)
            : this(databaseTypes, SqlClientFactory.Instance, generator, quoter, logger, options, connectionStringAccessor, serviceProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerProcessor"/> class.
        /// </summary>
        /// <param name="databaseTypes">The database type names.</param>
        /// <param name="factory">The database provider factory.</param>
        /// <param name="generator">The migration generator.</param>
        /// <param name="quoter">The SQL quoter.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The processor options.</param>
        /// <param name="connectionStringAccessor">The connection string accessor.</param>
        /// <param name="serviceProvider">The service provider.</param>
        protected SqlServerProcessor(
            [NotNull, ItemNotNull] IEnumerable<string> databaseTypes,
            [NotNull] DbProviderFactory factory,
            [NotNull] IMigrationGenerator generator,
            [NotNull] IQuoter quoter,
            [NotNull] ILogger logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] IServiceProvider serviceProvider)
            : base(() => factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
            _serviceProvider = serviceProvider;
            var dbTypes = databaseTypes.ToList();
            DatabaseType = dbTypes.First();
            DatabaseTypeAliases = dbTypes.Skip(1).ToList();
        }

        /// <summary>
        /// Returns a safe schema name, defaulting to "dbo" if null or empty.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <returns>The safe schema name.</returns>
        private static string SafeSchemaName(string schemaName)
        {
            return string.IsNullOrEmpty(schemaName) ? "dbo" : FormatHelper.FormatSqlEscape(schemaName);
        }

        /// <inheritdoc />
        public override void BeginTransaction()
        {
            base.BeginTransaction();
            Logger.LogSql("BEGIN TRANSACTION");
        }

        /// <inheritdoc />
        public override void CommitTransaction()
        {
            base.CommitTransaction();
            Logger.LogSql("COMMIT TRANSACTION");
        }

        /// <inheritdoc />
        public override void RollbackTransaction()
        {
            if (Transaction == null)
            {
                return;
            }

            base.RollbackTransaction();
            Logger.LogSql("ROLLBACK TRANSACTION");
        }

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return string.IsNullOrEmpty(schemaName) ? "dbo" : FormatHelper.FormatSqlEscape(schemaName);
        }

        /// <inheritdoc />
        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM [{0}].[{1}]", FormatSchemaName(schemaName), tableName);
        }

        /// <inheritdoc />
        protected override void Process(string sql)
        {
            Logger.LogSql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
            {
                return;
            }

            EnsureConnectionIsOpen();

            if (ContainsGo(sql))
            {
                ExecuteBatchNonQuery(sql);
            }
            else
            {
                ExecuteNonQuery(sql);
            }
        }

        private bool ContainsGo(string sql)
        {
            var containsGo = false;
            var parser = _serviceProvider?.GetService<SqlServerBatchParser>() ?? new SqlServerBatchParser();
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
                catch (Exception ex)
                {
                    using (var message = new StringWriter())
                    {
                        ReThrowWithSql(ex, sql);
                    }
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql)
        {
            var sqlBatch = string.Empty;

            try
            {
                var parser = _serviceProvider?.GetService<SqlServerBatchParser>() ?? new SqlServerBatchParser();
                parser.SqlText += (sender, args) => sqlBatch = args.SqlText.Trim();
                parser.SpecialToken += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(sqlBatch))
                    {
                        return;
                    }

                    if (args.Opaque is GoSearcher.GoSearcherParameters goParams)
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            for (var i = 0; i != goParams.Count; ++i)
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    sqlBatch = null;
                };

                using (var source = new TextReaderSource(new StringReader(sql), true))
                {
                    parser.Process(source, stripComments: Options.StripComments);
                }

                if (!string.IsNullOrEmpty(sqlBatch))
                {
                    using (var command = CreateCommand(sqlBatch))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ReThrowWithSql(ex, string.IsNullOrEmpty(sqlBatch) ? sql : sqlBatch);
            }
        }

        /// <inheritdoc />
        public override void Process(PerformDBOperationExpression expression)
        {
            var message = string.IsNullOrEmpty(expression.Description)
                ? "Performing DB Operation"
                : $"Performing DB Operation: {expression.Description}";
            Logger.LogSay(message);

            if (Options.PreviewOnly)
            {
                return;
            }

            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }
    }
}
