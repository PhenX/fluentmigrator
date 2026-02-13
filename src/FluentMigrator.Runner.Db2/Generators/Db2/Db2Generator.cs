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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Generic;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Generators.DB2
{
    /// <summary>
    /// The DB2 SQL generator for FluentMigrator.
    /// </summary>
    public class Db2Generator : GenericGenerator
    {
        /// <inheritdoc />
        public Db2Generator()
            : this(new Db2Quoter())
        {
        }

        /// <inheritdoc />
        public Db2Generator(
            Db2Quoter quoter)
            : this(
                quoter,
                new OptionsWrapper<GeneratorOptions>(new GeneratorOptions()))
        {
        }

        /// <inheritdoc />
        public Db2Generator(
            Db2Quoter quoter,
            IOptions<GeneratorOptions> generatorOptions)
            : base(new Db2Column(quoter), quoter, new EmptyDescriptionGenerator(), generatorOptions)
        {
        }

        /// <inheritdoc />
        public override string Generate(Expressions.AlterDefaultConstraintExpression expression)
        {
            return FormatStatement(
                "ALTER TABLE {0} ALTER COLUMN {1} SET DEFAULT {2}",
                Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                Quoter.QuoteColumnName(expression.ColumnName),
                ((Db2Column)Column).FormatAlterDefaultValue(expression.ColumnName, expression.DefaultValue));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteDefaultConstraintExpression expression)
        {
            return FormatStatement(
                "ALTER TABLE {0} ALTER COLUMN {1} DROP DEFAULT",
                Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                Quoter.QuoteColumnName(expression.ColumnName));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteColumnExpression expression)
        {
            var builder = new StringBuilder();
            if (expression.ColumnNames.Count == 0 || string.IsNullOrEmpty(expression.ColumnNames.First()))
            {
                return string.Empty;
            }

            builder.AppendFormat("ALTER TABLE {0}", Quoter.QuoteTableName(expression.TableName, expression.SchemaName));
            foreach (var column in expression.ColumnNames)
            {
                builder.AppendFormat(" DROP COLUMN {0}", Quoter.QuoteColumnName(column));
            }

            AppendSqlStatementEndToken(builder);

            return builder.ToString();
        }

        /// <inheritdoc />
        public override string Generate(Expressions.CreateColumnExpression expression)
        {
            expression.Column.AdditionalFeatures.Add(new KeyValuePair<string, object>("IsCreateColumn", true));

            return FormatStatement(
                "ALTER TABLE {0} ADD COLUMN {1}",
                Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                Column.Generate(expression.Column));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.CreateForeignKeyExpression expression)
        {
            if (expression.ForeignKey.PrimaryColumns.Count != expression.ForeignKey.ForeignColumns.Count)
            {
                throw new ArgumentException("Number of primary columns and secondary columns must be equal");
            }

            var keyName = string.IsNullOrEmpty(expression.ForeignKey.Name)
                ? Column.GenerateForeignKeyName(expression.ForeignKey)
                : expression.ForeignKey.Name;
            var keyWithSchema = Quoter.QuoteConstraintName(keyName, expression.ForeignKey.ForeignTableSchema);

            var primaryColumns = expression.ForeignKey.PrimaryColumns.Aggregate(new StringBuilder(), (acc, col) =>
            {
                var separator = acc.Length == 0 ? string.Empty : ", ";
                return acc.AppendFormat("{0}{1}", separator, Quoter.QuoteColumnName(col));
            });

            var foreignColumns = expression.ForeignKey.ForeignColumns.Aggregate(new StringBuilder(), (acc, col) =>
            {
                var separator = acc.Length == 0 ? string.Empty : ", ";
                return acc.AppendFormat("{0}{1}", separator, Quoter.QuoteColumnName(col));
            });

            return FormatStatement(
                "ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3} ({4}){5}",
                Quoter.QuoteTableName(expression.ForeignKey.ForeignTable, expression.ForeignKey.ForeignTableSchema),
                keyWithSchema,
                foreignColumns,
                Quoter.QuoteTableName(expression.ForeignKey.PrimaryTable, expression.ForeignKey.PrimaryTableSchema),
                primaryColumns,
                Column.FormatCascade("DELETE", expression.ForeignKey.OnDelete));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.CreateConstraintExpression expression)
        {
            var constraintName = Quoter.QuoteConstraintName(expression.Constraint.ConstraintName, expression.Constraint.SchemaName);

            var constraintType = expression.Constraint.IsPrimaryKeyConstraint ? "PRIMARY KEY" : "UNIQUE";
            var quotedNames = expression.Constraint.Columns.Select(q => Quoter.QuoteColumnName(q));
            var columnList = string.Join(", ", quotedNames.ToArray());

            return FormatStatement(
                "ALTER TABLE {0} ADD CONSTRAINT {1} {2} ({3})",
                Quoter.QuoteTableName(expression.Constraint.TableName, expression.Constraint.SchemaName),
                constraintName,
                constraintType,
                columnList);
        }

        /// <inheritdoc />
        public override string Generate(Expressions.CreateIndexExpression expression)
        {
            var indexWithSchema = Quoter.QuoteIndexName(expression.Index.Name, expression.Index.SchemaName);

            var columnList = expression.Index.Columns.Aggregate(new StringBuilder(), (item, itemToo) =>
            {
                var accumulator = item.Length == 0 ? string.Empty : ", ";
                var direction = itemToo.Direction == Direction.Ascending ? string.Empty : " DESC";

                return item.AppendFormat("{0}{1}{2}", accumulator, Quoter.QuoteColumnName(itemToo.Name), direction);
            });

            return FormatStatement(
                "CREATE {0}INDEX {1} ON {2} ({3})",
                expression.Index.IsUnique ? "UNIQUE " : string.Empty,
                indexWithSchema,
                Quoter.QuoteTableName(expression.Index.TableName, expression.Index.SchemaName),
                columnList);
        }

        /// <inheritdoc />
        public override string GeneratorId => GeneratorIdConstants.DB2;

        /// <inheritdoc />
        public override List<string> GeneratorIdAliases => new List<string> { GeneratorIdConstants.DB2 };

        /// <inheritdoc />
        public override string Generate(Expressions.CreateSchemaExpression expression)
        {
            return FormatStatement("CREATE SCHEMA {0}", Quoter.QuoteSchemaName(expression.SchemaName));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteTableExpression expression)
        {
            if (expression.IfExists)
            {
                if (expression.SchemaName == null)
                {
                    return CompatibilityMode.HandleCompatibility("Db2 needs schema name to safely handle if exists");
                }

                return FormatStatement(
                    "IF( EXISTS(SELECT 1 FROM SYSCAT.TABLES WHERE TABSCHEMA = '{0}' AND TABNAME = '{1}')) THEN DROP TABLE {2} END IF",
                    Quoter.QuoteSchemaName(expression.SchemaName),
                    Quoter.QuoteTableName(expression.TableName),
                    Quoter.QuoteTableName(expression.TableName, expression.SchemaName)
                );
            }

            return FormatStatement(DropTable, Quoter.QuoteTableName(expression.TableName, expression.SchemaName));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteIndexExpression expression)
        {
            var indexWithSchema = Quoter.QuoteIndexName(expression.Index.Name, expression.Index.SchemaName);
            return FormatStatement("DROP INDEX {0}", indexWithSchema);
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteSchemaExpression expression)
        {
            return FormatStatement("DROP SCHEMA {0} RESTRICT", Quoter.QuoteSchemaName(expression.SchemaName));
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteConstraintExpression expression)
        {
            var constraintName = Quoter.QuoteConstraintName(expression.Constraint.ConstraintName, expression.Constraint.SchemaName);

            return FormatStatement(
                "ALTER TABLE {0} DROP CONSTRAINT {1}",
                Quoter.QuoteTableName(expression.Constraint.TableName, expression.Constraint.SchemaName),
                constraintName);
        }

        /// <inheritdoc />
        public override string Generate(Expressions.DeleteForeignKeyExpression expression)
        {
            var constraintName = Quoter.QuoteConstraintName(expression.ForeignKey.Name, expression.ForeignKey.ForeignTableSchema);

            return FormatStatement(
                "ALTER TABLE {0} DROP FOREIGN KEY {1}",
                Quoter.QuoteTableName(expression.ForeignKey.ForeignTable, expression.ForeignKey.ForeignTableSchema),
                constraintName);
        }

        /// <inheritdoc />
        public override string Generate(Expressions.RenameColumnExpression expression)
        {
            return CompatibilityMode.HandleCompatibility("This feature not directly supported by most versions of DB2.");
        }

        /// <inheritdoc />
        public override string Generate(Expressions.InsertDataExpression expression)
        {
            var sb = new StringBuilder();
            foreach (var row in expression.Rows)
            {
                var columnList = row.Aggregate(new StringBuilder(), (acc, rowVal) =>
                {
                    var accumulator = acc.Length == 0 ? string.Empty : ", ";
                    return acc.AppendFormat("{0}{1}", accumulator, Quoter.QuoteColumnName(rowVal.Key));
                });

                var dataList = row.Aggregate(new StringBuilder(), (acc, rowVal) =>
                {
                    var accumulator = acc.Length == 0 ? string.Empty : ", ";
                    return acc.AppendFormat("{0}{1}", accumulator, Quoter.QuoteValue(rowVal.Value));
                });

                var separator = sb.Length == 0 ? string.Empty : " ";

                sb.AppendFormat(
                    "{0}INSERT INTO {1} ({2}) VALUES ({3})",
                    separator,
                    Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                    columnList,
                    dataList);

                AppendSqlStatementEndToken(sb);
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        public override string Generate(Expressions.AlterColumnExpression expression)
        {
            try
            {
                // throws an exception of an attempt is made to alter an identity column, as it is not supported by most version of DB2.
                return FormatStatement("ALTER TABLE {0} {1}", Quoter.QuoteTableName(expression.TableName, expression.SchemaName), ((Db2Column)Column).GenerateAlterClause(expression.Column));
            }
            catch (NotSupportedException e)
            {
                return CompatibilityMode.HandleCompatibility(e.Message);
            }
        }

        /// <inheritdoc />
        public override string Generate(Expressions.AlterSchemaExpression expression)
        {
            return CompatibilityMode.HandleCompatibility("This feature not directly supported by most versions of DB2.");
        }

        /// <inheritdoc />
        public override string Generate(UpsertDataExpression expression)
        {
            if (expression.IgnoreInsertIfExists)
            {
                // DB2 MERGE with INSERT only (no UPDATE clause for INSERT IGNORE mode)
                return GenerateDB2MergeInsertOnly(expression);
            }

            // DB2 MERGE statement (similar to SQL Server)
            return GenerateDB2Merge(expression);
        }

        /// <summary>
        /// Generates a DB2 MERGE statement for full upsert functionality
        /// </summary>
        /// <param name="expression">The upsert expression</param>
        /// <returns>The DB2 MERGE SQL statement</returns>
        protected virtual string GenerateDB2Merge(UpsertDataExpression expression)
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
                var valuesList = string.Join(", ", values);

                sb.AppendLine($"USING (VALUES ({valuesList})) AS source ({columnNames})");

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
        /// Generates a DB2 MERGE statement for INSERT IGNORE mode (insert only if not exists)
        /// </summary>
        /// <param name="expression">The upsert expression</param>
        /// <returns>The DB2 MERGE SQL statement</returns>
        protected virtual string GenerateDB2MergeInsertOnly(UpsertDataExpression expression)
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
                var valuesList = string.Join(", ", values);

                sb.AppendLine($"USING (VALUES ({valuesList})) AS source ({columnNames})");

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
