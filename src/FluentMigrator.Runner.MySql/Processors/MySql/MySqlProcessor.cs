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

using System;
using System.Collections.Generic;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.Generators.MySql;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.MySql
{
    /// <summary>
    /// The MySQL processor for FluentMigrator.
    /// </summary>
    public class MySqlProcessor : GenericProcessorBase
    {
        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.MySql;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { ProcessorIdConstants.MariaDB };

        /// <inheritdoc />
        protected MySqlProcessor(
            [NotNull] MySqlDbFactory factory,
            [NotNull] IMigrationGenerator generator,
            [NotNull] MySqlQuoter quoter,
            [NotNull] ILogger<MySqlProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
        }

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                  WHERE TABLE_SCHEMA = SCHEMA()
                  AND TABLE_NAME = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = SCHEMA()
                  AND TABLE_NAME = '{1}'
                  AND COLUMN_NAME = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            @"SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                  WHERE TABLE_SCHEMA = SCHEMA()
                  AND TABLE_NAME = '{1}'
                  AND CONSTRAINT_NAME = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            @"SELECT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS
                  WHERE TABLE_SCHEMA = SCHEMA()
                  AND TABLE_NAME = '{1}'
                  AND INDEX_NAME = '{2}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            @"SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = SCHEMA()
                  AND TABLE_NAME = '{1}'
                  AND COLUMN_NAME = '{2}'
                  AND COLUMN_DEFAULT LIKE '{3}'";

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
        public override void Process(RenameColumnExpression expression)
        {
            // MySql 8.0+ supports column rename without needing to know the column type
            if (Generator is MySql8Generator)
            {
                base.Process(expression);
                return;
            }

            if (Generator is not MySql4Generator mysql4Generator)
            {
                throw new InvalidOperationException("MySql4Generator is required for this operation");
            }

            var columnDefinitionSql = string.Format(@"
SELECT CONCAT(
          CAST(COLUMN_TYPE AS CHAR),
          IF(ISNULL(CHARACTER_SET_NAME),
             '',
             CONCAT(' CHARACTER SET ', CHARACTER_SET_NAME)),
          IF(ISNULL(COLLATION_NAME),
             '',
             CONCAT(' COLLATE ', COLLATION_NAME)),
          ' ',
          IF(IS_NULLABLE = 'NO', 'NOT NULL ', ''),
          IF(IS_NULLABLE = 'NO' AND COLUMN_DEFAULT IS NULL,
             '',
             CONCAT('DEFAULT ', IF(COLUMN_DEFAULT = 'NULL', 'NULL', QUOTE(COLUMN_DEFAULT)), ' ')),
          IF(COLUMN_COMMENT = '', '', CONCAT('COMMENT ', QUOTE(COLUMN_COMMENT), ' ')),
          UPPER(extra))
  FROM INFORMATION_SCHEMA.COLUMNS
 WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = database()", FormatHelper.FormatSqlEscape(expression.TableName), FormatHelper.FormatSqlEscape(expression.OldName));

            var fieldValue = Read(columnDefinitionSql).Tables[0].Rows[0][0];
            var columnDefinition = fieldValue as string;

            Process(mysql4Generator.GenerateWithoutEndStatement(expression) + " " + columnDefinition);
        }
    }
}
