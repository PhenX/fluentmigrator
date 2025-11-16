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

using FluentMigrator.Expressions;
using FluentMigrator.Generation;
using FluentMigrator.Model;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Generators.MySql
{
    /// <summary>
    /// The MySQL 5 SQL generator for FluentMigrator.
    /// </summary>
    public class MySql5Generator : MySql4Generator
    {
        /// <inheritdoc />
        public MySql5Generator()
            : this(new MySqlQuoter())
        {
        }

        /// <inheritdoc />
        public MySql5Generator(
            [NotNull] MySqlQuoter quoter)
            : this(quoter, new OptionsWrapper<GeneratorOptions>(new GeneratorOptions()))
        {
        }

        /// <inheritdoc />
        public MySql5Generator(
            [NotNull] MySqlQuoter quoter,
            [NotNull] IMySqlTypeMap typeMap)
            : this(quoter, typeMap, new OptionsWrapper<GeneratorOptions>(new GeneratorOptions()))
        {
        }

        /// <inheritdoc />
        public MySql5Generator(
            [NotNull] MySqlQuoter quoter,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : this(new MySqlColumn(new MySql5TypeMap(), quoter), quoter, new EmptyDescriptionGenerator(), generatorOptions)
        {
        }

        /// <inheritdoc />
        public MySql5Generator(
            [NotNull] MySqlQuoter quoter,
            [NotNull] IMySqlTypeMap typeMap,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : base(new MySqlColumn(typeMap, quoter), quoter, new EmptyDescriptionGenerator(), generatorOptions)
        {
        }

        /// <inheritdoc />
        protected MySql5Generator(
            [NotNull] IColumn column,
            [NotNull] IQuoter quoter,
            [NotNull] IDescriptionGenerator descriptionGenerator,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : base(column, quoter, descriptionGenerator, generatorOptions)
        {
        }

        /// <inheritdoc />
        public override string GeneratorId => GeneratorIdConstants.MySql5;

        /// <inheritdoc />
        public override List<string> GeneratorIdAliases =>
        [
            GeneratorIdConstants.MySql5, GeneratorIdConstants.MySql, GeneratorIdConstants.MariaDB
        ];

        /// <inheritdoc />
        public override string Generate(UpsertDataExpression expression)
        {
            if (expression.Rows.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var tableName = Quoter.QuoteTableName(expression.TableName, expression.SchemaName);

            // Get all column names from the first row
            var firstRow = expression.Rows.First();
            var allColumns = firstRow.Select(kvp => kvp.Key).ToList();

            // Handle different UPSERT modes
            if (expression.IgnoreInsertIfExists)
            {
                sb.AppendLine($"INSERT IGNORE INTO {tableName}");
            }
            else
            {
                sb.AppendLine($"INSERT INTO {tableName}");
            }
            
            sb.Append("(");
            sb.Append(string.Join(", ", allColumns.Select(c => Quoter.QuoteColumnName(c))));
            sb.AppendLine(")");
            sb.Append("VALUES");

            // Generate VALUES clause for all rows
            for (var i = 0; i < expression.Rows.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                
                sb.AppendLine();
                sb.Append("(");
                
                var row = expression.Rows[i];
                var values = new List<string>();
                
                foreach (var column in allColumns)
                {
                    var kvp = row.FirstOrDefault(r => r.Key == column);
                    values.Add(Quoter.QuoteValue(kvp.Value));
                }
                
                sb.Append(string.Join(", ", values));
                sb.Append(")");
            }

            // Add ON DUPLICATE KEY UPDATE clause if not in ignore mode
            if (!expression.IgnoreInsertIfExists)
            {
                sb.AppendLine();
                sb.Append("ON DUPLICATE KEY UPDATE");

                // Determine which columns to update
                var updateColumns = GetUpdateColumns(expression, allColumns);
                
                var updateClauses = new List<string>();
                foreach (var column in updateColumns)
                {
                    var quotedColumn = Quoter.QuoteColumnName(column);
                    
                    // Check if this column has a specific update value
                    if (expression.UpdateValues != null)
                    {
                        var updateValue = expression.UpdateValues.FirstOrDefault(uv => uv.Key == column);
                        if (updateValue.Key != null)
                        {
                            updateClauses.Add($"{quotedColumn} = {Quoter.QuoteValue(updateValue.Value)}");
                            continue;
                        }
                    }
                    
                    // Default: use VALUES() function to reference the new value
                    updateClauses.Add($"{quotedColumn} = VALUES({quotedColumn})");
                }

                if (updateClauses.Any())
                {
                    sb.AppendLine();
                    sb.Append("    ");
                    sb.Append(string.Join($",{System.Environment.NewLine}    ", updateClauses));
                }
            }

            return FormatStatement(sb.ToString());
        }

        /// <summary>
        /// Gets the columns to update in the ON DUPLICATE KEY UPDATE clause
        /// </summary>
        /// <param name="expression">The upsert expression</param>
        /// <param name="allColumns">All columns from the data</param>
        /// <returns>List of columns to update</returns>
        private List<string> GetUpdateColumns(UpsertDataExpression expression, List<string> allColumns)
        {
            // If specific update columns are specified, use those
            if (expression.UpdateColumns != null && expression.UpdateColumns.Any())
            {
                return expression.UpdateColumns.ToList();
            }

            // If UpdateValues are specified, use those column names
            if (expression.UpdateValues != null && expression.UpdateValues.Any())
            {
                return expression.UpdateValues.Select(uv => uv.Key).ToList();
            }

            // Default: update all columns except the match columns
            return allColumns.Except(expression.MatchColumns).ToList();
        }
    }
}
