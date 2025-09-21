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
using System.Text;

using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.Generators.Oracle;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.Oracle
{
    /// <summary>
    /// Base class for Oracle processors in FluentMigrator.
    /// </summary>
    public abstract class OracleProcessorBase : GenericProcessorBase
    {
        /// <inheritdoc />
        protected OracleProcessorBase(
            [NotNull] string databaseType,
            [NotNull] OracleBaseDbFactory factory,
            [NotNull] IMigrationGenerator generator,
            [NotNull] ILogger logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, ((OracleGenerator) generator).Quoter, logger, options.Value, connectionStringAccessor)
        {
            DatabaseType = databaseType;
        }

        /// <inheritdoc />
        public override string DatabaseType { get; }

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>() { ProcessorIdConstants.Oracle };

        /// <inheritdoc />
        protected override string SchemaExistsQuery => "SELECT 1 FROM ALL_USERS WHERE upper(USERNAME) = '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT 1 FROM ALL_TABLES WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}'";

        /// <inheritdoc />
        protected override string TableWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TABLES WHERE upper(TABLE_NAME) = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT 1 FROM ALL_TAB_COLUMNS WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ColumnWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TAB_COLUMNS WHERE upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT 1 FROM ALL_CONSTRAINTS WHERE upper(OWNER) = '{0}' AND upper(CONSTRAINT_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_CONSTRAINTS WHERE upper(CONSTRAINT_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT 1 FROM ALL_INDEXES WHERE upper(OWNER) = '{0}' AND upper(INDEX_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string IndexWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_INDEXES WHERE upper(INDEX_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "SELECT 1 FROM ALL_SEQUENCES WHERE upper(SEQUENCE_OWNER) = '{0}' AND upper(SEQUENCE_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string SequenceWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_SEQUENCES WHERE upper(SEQUENCE_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "SELECT 1 FROM ALL_TAB_COLUMNS WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}' AND DATA_DEFAULT IS NOT NULL";

        /// <inheritdoc />
        protected override string DefaultValueWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TAB_COLUMNS WHERE upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}' AND DATA_DEFAULT IS NOT NULL";

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return base.FormatSchemaName(schemaName)?.ToUpper();
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return base.FormatName(name)?.ToUpper();
        }

        /// <summary>
        /// Splits a SQL script into individual statements, taking into account Oracle-specific syntax rules.
        /// <remarks>We could use <see cref="SqlBatchParser"/> but it does not handle multiple lines strings.</remarks>
        /// </summary>
        private static List<string> SplitOracleSqlStatements(string sqlScript)
        {
            var statements = new List<string>();
            var currentStatement = new StringBuilder();
            var inString = false;
            var inIdentifier = false;
            var inSingleLineComment = false;
            var inMultiLineComment = false;
            var prevChar = '\0';

            foreach (var c in sqlScript)
            {
                if (inSingleLineComment)
                {
                    currentStatement.Append(c);
                    if (c == '\n')
                    {
                        inSingleLineComment = false;
                    }
                    continue;
                }

                if (inMultiLineComment)
                {
                    currentStatement.Append(c);
                    if (prevChar == '*' && c == '/')
                    {
                        inMultiLineComment = false;
                    }
                    prevChar = c;
                    continue;
                }

                if (inString)
                {
                    currentStatement.Append(c);
                    if (c == '\'' && prevChar != '\\')
                    {
                        inString = false;
                    }
                    prevChar = c;
                    continue;
                }

                if (inIdentifier)
                {
                    currentStatement.Append(c);
                    if (c == '"')
                    {
                        inIdentifier = false;
                    }
                    prevChar = c;
                    continue;
                }

                switch (c)
                {
                    // Check for comment start
                    case '-' when prevChar == '-':
                        inSingleLineComment = true;
                        currentStatement.Append(c);
                        continue;
                    case '*' when prevChar == '/':
                        inMultiLineComment = true;
                        currentStatement.Append(c);
                        continue;
                    // Check for string start
                    case '\'':
                        inString = true;
                        currentStatement.Append(c);
                        prevChar = c;
                        continue;
                    // Check for inIdentifier start
                    case '"':
                        inIdentifier = true;
                        currentStatement.Append(c);
                        prevChar = c;
                        continue;
                    // Check for statement terminator
                    case ';':
                        statements.Add(currentStatement.ToString().TrimEnd(';').Trim());
                        currentStatement.Clear();
                        prevChar = '\0';
                        continue;
                    default:
                        currentStatement.Append(c);
                        prevChar = c;
                        break;
                }
            }

            // Add any remaining content
            if (currentStatement.Length > 0)
            {
                statements.Add(currentStatement.ToString());
            }

            return statements;
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

            var batches = SplitOracleSqlStatements(sql);

            foreach (var batch in batches)
            {
                using (var command = CreateCommand(batch))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        ReThrowWithSql(ex, batch);
                    }
                }
            }
        }
    }
}
