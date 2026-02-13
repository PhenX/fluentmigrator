#region License
// Copyright (c) 2007-2024, Fluent Migrator Project
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

using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentMigrator.Exceptions;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Generic;
using FluentMigrator.Runner.Processors.Snowflake;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Generators.Snowflake
{
    /// <summary>
    /// The Snowflake SQL generator for FluentMigrator.
    /// </summary>
    public class SnowflakeGenerator : GenericGenerator
    {
        /// <inheritdoc />
        public SnowflakeGenerator(
            [NotNull] SnowflakeOptions sfOptions)
            : this(sfOptions, new OptionsWrapper<GeneratorOptions>(new GeneratorOptions())) { }

        /// <inheritdoc />
        public SnowflakeGenerator(
            [NotNull] SnowflakeOptions sfOptions,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : this(new SnowflakeQuoter(sfOptions.QuoteIdentifiers), sfOptions, generatorOptions) { }

        /// <inheritdoc />
        public SnowflakeGenerator(
            [NotNull] SnowflakeQuoter quoter,
            [NotNull] SnowflakeOptions sfOptions,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : base(new SnowflakeColumn(sfOptions), quoter, new SnowflakeDescriptionGenerator(quoter), generatorOptions)
        {
        }

        /// <inheritdoc />
        public override string AlterColumn => "ALTER TABLE {0} ALTER {1}";

        /// <inheritdoc />
        public override string Generate(AlterDefaultConstraintExpression expression)
        {
            throw new DatabaseOperationNotSupportedException("Snowflake database does not support adding or changing default constraint after column has been created.");
        }

        /// <inheritdoc />
        public override string Generate(DeleteDefaultConstraintExpression expression)
        {
            return FormatStatement($"ALTER TABLE {Quoter.QuoteTableName(expression.TableName, expression.SchemaName)} ALTER COLUMN {Quoter.QuoteColumnName(expression.ColumnName)} DROP DEFAULT");
        }

        /// <inheritdoc />
        public override string GeneratorId => GeneratorIdConstants.Snowflake;

        /// <inheritdoc />
        public override List<string> GeneratorIdAliases => new List<string> { GeneratorIdConstants.Snowflake };

        /// <inheritdoc />
        public override string Generate(CreateSchemaExpression expression)
        {
            return FormatStatement(CreateSchema, Quoter.QuoteSchemaName(expression.SchemaName));
        }

        /// <inheritdoc />
        public override string Generate(DeleteSchemaExpression expression)
        {
            return FormatStatement(DropSchema, Quoter.QuoteSchemaName(expression.SchemaName));
        }

        /// <inheritdoc />
        public override string Generate(DeleteTableExpression expression)
        {
            return FormatStatement($"DROP TABLE{(expression.IfExists ? " IF EXISTS" : "")} {Quoter.QuoteTableName(expression.TableName, expression.SchemaName)}");
        }

        /// <inheritdoc />
        public override string Generate(CreateTableExpression expression)
        {
            if (expression.Columns.Any(x => x.Expression != null))
            {
                CompatibilityMode.HandleCompatibility("Computed columns are not supported");
            }
            return base.Generate(expression);
        }

        /// <inheritdoc />
        public override string Generate(CreateColumnExpression expression)
        {
            if (expression.Column.Expression != null)
            {
                CompatibilityMode.HandleCompatibility("Computed columns are not supported");
            }
            return base.Generate(expression);
        }

        /// <inheritdoc />
        public override string Generate(AlterColumnExpression expression)
        {
            if (expression.Column.Expression != null)
            {
                CompatibilityMode.HandleCompatibility("Computed columns are not supported");
            }
            if (!(expression.Column.DefaultValue is ColumnDefinition.UndefinedDefaultValue))
            {
                throw new DatabaseOperationNotSupportedException("Snowflake database does not support adding or changing default constraint after column has been created.");
            }

            var errors = ValidateAdditionalFeatureCompatibility(expression.Column.AdditionalFeatures);
            if (!string.IsNullOrEmpty(errors))
            {
                return errors;
            }

            return FormatStatement(AlterColumn, Quoter.QuoteTableName(expression.TableName, expression.SchemaName), ((SnowflakeColumn)Column).GenerateAlterColumn(expression.Column));
        }

        /// <inheritdoc />
        public override string Generate(RenameTableExpression expression)
        {
            return FormatStatement($"ALTER TABLE {Quoter.QuoteTableName(expression.OldName, expression.SchemaName)} RENAME TO {Quoter.QuoteTableName(expression.NewName, expression.SchemaName)}");
        }

        /// <inheritdoc />
        public override string Generate(AlterSchemaExpression expression)
        {
            return FormatStatement($"ALTER TABLE {Quoter.QuoteTableName(expression.TableName, expression.SourceSchemaName)} RENAME TO {Quoter.QuoteTableName(expression.TableName, expression.DestinationSchemaName)}");
        }

        /// <inheritdoc />
        public override string Generate(CreateIndexExpression expression)
        {
            return CompatibilityMode.HandleCompatibility("Indices not supported");
        }

        /// <inheritdoc />
        public override string Generate(DeleteIndexExpression expression)
        {
            return CompatibilityMode.HandleCompatibility("Indices not supported");
        }

        /// <inheritdoc />
        public override string Generate(CreateSequenceExpression expression)
        {
            var result = new StringBuilder("CREATE SEQUENCE ");
            var seq = expression.Sequence;
            result.AppendFormat(Quoter.QuoteSequenceName(seq.Name, seq.SchemaName));

            if (seq.StartWith.HasValue)
            {
                result.AppendFormat(" START {0}", seq.StartWith);
            }

            if (seq.Increment.HasValue)
            {
                result.AppendFormat(" INCREMENT {0}", seq.Increment);
            }

            AppendSqlStatementEndToken(result);

            return result.ToString();
        }

        /// <inheritdoc />
        public override string Generate(UpsertDataExpression expression)
        {
            if (expression.IgnoreInsertIfExists)
            {
                // Snowflake MERGE with INSERT only (no UPDATE clause for INSERT IGNORE mode)
                return GenerateSnowflakeMergeInsertOnly(expression);
            }

            // Snowflake MERGE statement (similar to SQL Server/Oracle)
            return GenerateSnowflakeMerge(expression);
        }

        /// <summary>
        /// Generates a Snowflake MERGE statement for full upsert functionality
        /// </summary>
        /// <param name="expression">The upsert expression</param>
        /// <returns>The Snowflake MERGE SQL statement</returns>
        protected virtual string GenerateSnowflakeMerge(UpsertDataExpression expression)
        {
            var sb = new StringBuilder();
            var tableName = Quoter.QuoteTableName(expression.TableName, expression.SchemaName);

            foreach (var row in expression.Rows)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine($"MERGE INTO {tableName} AS target");

                // Build source data using VALUES clause
                var columns = row.Select(kvp => kvp.Key).ToList();
                var values = row.Select(kvp => Quoter.QuoteValue(kvp.Value)).ToList();
                var columnNames = string.Join(", ", columns.Select(c => Quoter.QuoteColumnName(c)));
                
                // Snowflake uses a subquery for source data
                var sourceColumns = columns.Select((col, index) => $"{values[index]} AS {Quoter.QuoteColumnName(col)}");
                sb.AppendLine($"USING (SELECT {string.Join(", ", sourceColumns)}) AS source");

                // Build ON clause for match columns
                var matchConditions = expression.MatchColumns.Select(col =>
                    $"target.{Quoter.QuoteColumnName(col)} = source.{Quoter.QuoteColumnName(col)}");
                sb.AppendLine($"ON ({string.Join(" AND ", matchConditions)})");

                // Build WHEN MATCHED clause (UPDATE)
                var updateItems = new List<string>();
                
                if (expression.UpdateValues?.Any() == true)
                {
                    // Use specific update values (supports RawSql)
                    foreach (var updateValue in expression.UpdateValues)
                    {
                        updateItems.Add($"{Quoter.QuoteColumnName(updateValue.Key)} = {Quoter.QuoteValue(updateValue.Value)}");
                    }
                }
                else if (expression.UpdateColumns?.Any() == true)
                {
                    // Use specified update columns (exclude match columns)
                    foreach (var column in expression.UpdateColumns)
                    {
                        if (!expression.MatchColumns.Contains(column))
                        {
                            var columnValue = row.FirstOrDefault(kvp => kvp.Key == column);
                            if (!columnValue.Equals(default(KeyValuePair<string, object>)))
                            {
                                updateItems.Add($"{Quoter.QuoteColumnName(columnValue.Key)} = source.{Quoter.QuoteColumnName(columnValue.Key)}");
                            }
                        }
                    }
                }
                else
                {
                    // Update all columns except match columns
                    foreach (var kvp in row)
                    {
                        if (!expression.MatchColumns.Contains(kvp.Key))
                        {
                            updateItems.Add($"{Quoter.QuoteColumnName(kvp.Key)} = source.{Quoter.QuoteColumnName(kvp.Key)}");
                        }
                    }
                }

                if (updateItems.Any())
                {
                    sb.AppendLine($"WHEN MATCHED THEN");
                    sb.AppendLine($"    UPDATE SET {string.Join(", ", updateItems)}");
                }

                // Build WHEN NOT MATCHED clause (INSERT)
                sb.AppendLine($"WHEN NOT MATCHED THEN");
                sb.AppendLine($"    INSERT ({columnNames})");
                sb.Append($"    VALUES ({string.Join(", ", columns.Select(c => $"source.{Quoter.QuoteColumnName(c)}"))})");

                AppendSqlStatementEndToken(sb);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a Snowflake MERGE statement for INSERT IGNORE mode (insert only if not exists)
        /// </summary>
        /// <param name="expression">The upsert expression</param>
        /// <returns>The Snowflake MERGE SQL statement</returns>
        protected virtual string GenerateSnowflakeMergeInsertOnly(UpsertDataExpression expression)
        {
            var sb = new StringBuilder();
            var tableName = Quoter.QuoteTableName(expression.TableName, expression.SchemaName);

            foreach (var row in expression.Rows)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine($"MERGE INTO {tableName} AS target");

                // Build source data using subquery
                var columns = row.Select(kvp => kvp.Key).ToList();
                var values = row.Select(kvp => Quoter.QuoteValue(kvp.Value)).ToList();
                var columnNames = string.Join(", ", columns.Select(c => Quoter.QuoteColumnName(c)));
                
                var sourceColumns = columns.Select((col, index) => $"{values[index]} AS {Quoter.QuoteColumnName(col)}");
                sb.AppendLine($"USING (SELECT {string.Join(", ", sourceColumns)}) AS source");

                // Build ON clause for match columns
                var matchConditions = expression.MatchColumns.Select(col =>
                    $"target.{Quoter.QuoteColumnName(col)} = source.{Quoter.QuoteColumnName(col)}");
                sb.AppendLine($"ON ({string.Join(" AND ", matchConditions)})");

                // Only WHEN NOT MATCHED clause (INSERT IGNORE - no updates)
                sb.AppendLine($"WHEN NOT MATCHED THEN");
                sb.AppendLine($"    INSERT ({columnNames})");
                sb.Append($"    VALUES ({string.Join(", ", columns.Select(c => $"source.{Quoter.QuoteColumnName(c)}"))})");

                AppendSqlStatementEndToken(sb);
            }

            return sb.ToString();
        }
    }
}
