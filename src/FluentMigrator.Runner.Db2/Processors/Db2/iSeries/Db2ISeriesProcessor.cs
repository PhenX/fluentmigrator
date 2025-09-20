#region License
//
// Copyright (c) 2007-2024, Fluent Migrator Project
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

using System.Collections.Generic;
using System.Linq;

using FluentMigrator.Runner.Generators.DB2.iSeries;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.DB2.iSeries
{
    /// <summary>
    /// The IBM Db2 for iSeries processor for FluentMigrator.
    /// </summary>
    public class Db2ISeriesProcessor : GenericProcessorBase
    {
        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.Db2ISeries;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { ProcessorIdConstants.IbmDb2ISeries, ProcessorIdConstants.DB2 };

        /// <inheritdoc />
        protected override string SchemaExistsQuery =>
            "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE {0}TABLE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE {0}TABLE_NAME = '{1}' AND COLUMN_NAME='{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE {0}TABLE_NAME = '{1}' AND CONSTRAINT_NAME='{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT NAME FROM INFORMATION_SCHEMA.SYSINDEXES WHERE {0}TABLE_NAME = '{1}' AND NAME = '{2}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "SELECT COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE {0}TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}' AND COLUMN_DEFAULT LIKE '{3}'";


        /// <inheritdoc />
        public Db2ISeriesProcessor(
            [NotNull] Db2ISeriesDbFactory factory,
            [NotNull] Db2ISeriesGenerator generator,
            [NotNull] Db2ISeriesQuoter quoter,
            [NotNull] ILogger<Db2ISeriesProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
        }

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return FormatToSafeName(schemaName);
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return FormatToSafeName(name);
        }

        /// <inheritdoc />
        public override bool TableExists(string schemaName, string tableName)
        {
            return base.TableExists(GetSchemaClause(schemaName, "TABLE_SCHEMA"), tableName);
        }

        /// <inheritdoc />
        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return base.ColumnExists(GetSchemaClause(schemaName, "TABLE_SCHEMA"), tableName, columnName);
        }

        /// <inheritdoc />
        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return base.ConstraintExists(GetSchemaClause(schemaName, "TABLE_SCHEMA"), tableName, constraintName);
        }

        /// <inheritdoc />
        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return base.IndexExists(GetSchemaClause(schemaName, "INDEX_SCHEMA"), tableName, indexName);
        }

        /// <inheritdoc />
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return base.DefaultValueExists(GetSchemaClause(schemaName, "TABLE_SCHEMA"), tableName, columnName, defaultValue);
        }

        /// <inheritdoc />
        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        private string GetSchemaClause(string schemaName, string infoTableName)
        {
            return string.IsNullOrEmpty(schemaName) ? string.Empty : $"{infoTableName} = '{FormatSchemaName(schemaName)}' AND ";
        }

        /// <summary>
        /// Formats the SQL name to a safe SQL value.
        /// </summary>
        /// <param name="sqlName">The SQL name.</param>
        /// <returns>The formatted SQL name.</returns>
        private string FormatToSafeName(string sqlName)
        {
            var rawName = Quoter.UnQuote(sqlName);

            return rawName.Contains('\'') ? FormatHelper.FormatSqlEscape(rawName) : rawName.ToUpper();
        }
    }
}
