#region License
//
// Copyright (c) 2018, Fluent Migrator Project
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

using FluentMigrator.Runner.Helpers;

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
using System.Data;
using System.Data.Common;
using System.IO;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;
using FluentMigrator.Runner.Generators.SqlServer;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.SqlServer
{
    /// <summary>
    /// The SQL Server 2000 processor for FluentMigrator.
    /// </summary>
    public class SqlServer2000Processor : GenericProcessorBase
    {
        [CanBeNull]
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_CATALOG = DB_NAME() AND AND TABLE_NAME = '{1}' AND CONSTRAINT_NAME = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT NULL FROM sysindexes WHERE name = '{0}'";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "SELECT * FROM INFORMATION_SCHEMA.SEQUENCES WHERE SEQUENCE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string ReadTableDataQuery => "SELECT * FROM [{0}]";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServer2000Processor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="generator">The migration generator.</param>
        /// <param name="quoter">The SQL Server 2000 quoter.</param>
        /// <param name="options">The processor options.</param>
        /// <param name="connectionStringAccessor">The connection string accessor.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public SqlServer2000Processor(
            [NotNull] ILogger<SqlServer2000Processor> logger,
            [NotNull] SqlServer2000Generator generator,
            [NotNull] SqlServer2000Quoter quoter,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] IServiceProvider serviceProvider)
            : this(SqlClientFactory.Instance, logger, generator, quoter, options, connectionStringAccessor, serviceProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServer2000Processor"/> class.
        /// </summary>
        /// <param name="factory">The database provider factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="generator">The migration generator.</param>
        /// <param name="quoter">The SQL Server 2000 quoter.</param>
        /// <param name="options">The processor options.</param>
        /// <param name="connectionStringAccessor">The connection string accessor.</param>
        /// <param name="serviceProvider">The service provider.</param>
        protected SqlServer2000Processor(
            DbProviderFactory factory,
            [NotNull] ILogger logger,
            [NotNull] SqlServer2000Generator generator,
            [NotNull] SqlServer2000Quoter quoter,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] IServiceProvider serviceProvider)
            : base(() => factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.SqlServer2000;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>() { ProcessorIdConstants.SqlServer };

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
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return false;
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
                    using (_ = new StringWriter())
                    {
                        ReThrowWithSql(ex, sql);
                    }
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql)
        {
            sql += "\nGO"; // make sure last batch is executed.
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
            }
            catch (Exception ex)
            {
                using (var message = new StringWriter())
                {
                    ReThrowWithSql(ex, string.IsNullOrEmpty(sqlBatch) ? sql : sqlBatch);
                }
            }
        }
    }
}
