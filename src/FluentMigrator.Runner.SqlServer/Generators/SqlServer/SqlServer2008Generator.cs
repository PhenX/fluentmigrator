#region License
//
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

using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentMigrator.Expressions;
using FluentMigrator.Generation;
using FluentMigrator.Infrastructure;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Model;
using FluentMigrator.SqlServer;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Generators.SqlServer
{
    /// <summary>
    /// The SQL Server 2008 SQL generator for FluentMigrator.
    /// </summary>
    public class SqlServer2008Generator : SqlServer2005Generator
    {
        private static readonly HashSet<string> _supportedAdditionalFeatures = new HashSet<string>
        {
            SqlServerExtensions.IndexColumnNullsDistinct,
        };

        /// <inheritdoc />
        public SqlServer2008Generator()
            : this(new SqlServer2008Quoter())
        {
        }

        /// <inheritdoc />
        public SqlServer2008Generator(
            [NotNull] SqlServer2008Quoter quoter)
            : this(quoter, new OptionsWrapper<GeneratorOptions>(new GeneratorOptions()))
        {
        }

        /// <inheritdoc />
        public SqlServer2008Generator(
            [NotNull] SqlServer2008Quoter quoter,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : this(
                new SqlServer2008Column(new SqlServer2008TypeMap(), quoter),
                quoter,
                new SqlServer2005DescriptionGenerator(),
                generatorOptions)
        {
        }

        /// <inheritdoc />
        protected SqlServer2008Generator(
            [NotNull] IColumn column,
            [NotNull] IQuoter quoter,
            [NotNull] IDescriptionGenerator descriptionGenerator,
            [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : base(column, quoter, descriptionGenerator, generatorOptions)
        {
        }

        /// <inheritdoc />
        public override string GeneratorId => GeneratorIdConstants.SqlServer2008;

        /// <inheritdoc />
        public override List<string> GeneratorIdAliases =>
            [GeneratorIdConstants.SqlServer2008, GeneratorIdConstants.SqlServer];

        /// <inheritdoc />
        public override bool IsAdditionalFeatureSupported(string feature)
        {
            return _supportedAdditionalFeatures.Contains(feature)
             || base.IsAdditionalFeatureSupported(feature);
        }

        /// <inheritdoc />
        public override string GetFilterString(CreateIndexExpression createIndexExpression)
        {
            var baseFilter = base.GetFilterString(createIndexExpression);
            var nullsDistinct = GetWithNullsDistinctString(createIndexExpression.Index);

            if (string.IsNullOrEmpty(baseFilter) && string.IsNullOrEmpty(nullsDistinct))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(nullsDistinct))
            {
                return baseFilter;
            }

            baseFilter = string.IsNullOrEmpty(baseFilter) ?
                $" WHERE {nullsDistinct}" :
                $" AND  {nullsDistinct}";

            return baseFilter;
        }

        /// <summary>
        /// Gets the SQL fragment for "nulls distinct" columns in unique indexes.
        /// </summary>
        /// <param name="index">The index definition.</param>
        /// <returns>The SQL fragment.</returns>
        protected string GetWithNullsDistinctString(IndexDefinition index)
        {
            bool? GetNullsDistinct(IndexColumnDefinition column)
            {
                return column.GetAdditionalFeature(SqlServerExtensions.IndexColumnNullsDistinct, (bool?)null);
            }

            var indexNullsDistinct = index.GetAdditionalFeature(SqlServerExtensions.IndexColumnNullsDistinct, (bool?)null);

            var nullDistinctColumns = index.Columns.Where(c => indexNullsDistinct != null || GetNullsDistinct(c) != null).ToList();
            if (nullDistinctColumns.Count != 0 && !index.IsUnique)
            {
                // Should never occur
                CompatibilityMode.HandleCompatibility("With nulls distinct can only be used for unique indexes");
                return string.Empty;
            }

            // The "Nulls (not) distinct" value of the column
            // takes higher precedence than the value of the index
            // itself.
            var conditions = nullDistinctColumns
                .Where(x => (GetNullsDistinct(x) ?? indexNullsDistinct ?? true) == false)
                .Select(c => $"{Quoter.QuoteColumnName(c.Name)} IS NOT NULL");

            var condition = string.Join(" AND ", conditions);
            if (condition.Length == 0)
                return string.Empty;

            return condition;
        }

        /// <inheritdoc />
        public override string GetWithOptions(ISupportAdditionalFeatures expression)
        {
            var items = new List<string>();
            var options = base.GetWithOptions(expression);

            if (!string.IsNullOrEmpty(options))
            {
                items.Add(options);
            }

            var dataCompressionType = expression.GetAdditionalFeature(SqlServerExtensions.DataCompression, (DataCompressionType)null);
            if (dataCompressionType != null)
            {
                items.Add($"DATA_COMPRESSION = {dataCompressionType}");
            }

            return string.Join(", ", items);
        }

        /// <summary>
        /// Generates a SQL Server MERGE statement that handles all rows in a single operation.
        /// </summary>
        /// <param name="expression">The upsert data expression containing rows to be inserted or updated.</param>
        /// <returns>A SQL Server MERGE statement for all rows.</returns>
        public override string Generate(UpsertDataExpression expression)
        {
            if (!expression.Rows.Any())
            {
                return string.Empty;
            }

            var output = new StringBuilder();

            // Get all unique column names from all rows
            var allColumnNames = expression.Rows
                .SelectMany(row => row.Select(r => r.Key))
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            var quotedColumnNames = allColumnNames.Select(name => Quoter.QuoteColumnName(name)).ToList();

            // Start the MERGE statement
            output.AppendLine($"MERGE {Quoter.QuoteTableName(expression.TableName, expression.SchemaName)} AS target");

            // Create the source with all row data using VALUES clause
            output.AppendLine("USING (VALUES");

            var rowValues = new List<string>();
            foreach (var row in expression.Rows)
            {
                var values = allColumnNames.Select(columnName =>
                    row.TryGetValue(columnName, out var value)
                        ? Quoter.QuoteValue(value)
                        : "NULL"
                ).ToList();

                rowValues.Add($"    ({string.Join(", ", values)})");
            }

            output.AppendLine(string.Join(",\n", rowValues));
            output.AppendLine($") AS source ({string.Join(", ", quotedColumnNames)})");

            // Create the ON condition using match columns
            var matchConditions = expression.MatchColumns.Select(matchColumn =>
                $"target.{Quoter.QuoteColumnName(matchColumn)} = source.{Quoter.QuoteColumnName(matchColumn)}"
            ).ToList();

            output.AppendLine($"ON ({string.Join(" AND ", matchConditions)})");

            if (expression.IgnoreInsertIfExists)
            {
                // INSERT IGNORE mode - only insert when not matched, no update
                output.AppendLine("WHEN NOT MATCHED THEN");
                output.AppendLine($"    INSERT ({string.Join(", ", quotedColumnNames)})");
                output.AppendLine($"    VALUES ({string.Join(", ", quotedColumnNames.Select(name => $"source.{name}"))})");
            }
            else
            {
                // Standard UPSERT mode - UPDATE when matched, INSERT when not matched
                if (expression.UpdateValues?.Any() == true)
                {
                    // Use specific update values (supports RawSql)
                    var updateAssignments = expression.UpdateValues.Select(updateValue =>
                        $"target.{Quoter.QuoteColumnName(updateValue.Key)} = {Quoter.QuoteValue(updateValue.Value)}"
                    ).ToList();

                    output.AppendLine("WHEN MATCHED THEN");
                    output.AppendLine($"    UPDATE SET {string.Join(", ", updateAssignments)}");
                }
                else
                {
                    // Use column-based update logic
                    var columnsToUpdate = expression.UpdateColumns?.Any() == true
                        ? expression.UpdateColumns
                        : allColumnNames.Where(column => !expression.MatchColumns.Contains(column)).ToList();

                    if (columnsToUpdate.Any())
                    {
                        var updateAssignments = columnsToUpdate.Select(column =>
                            $"target.{Quoter.QuoteColumnName(column)} = source.{Quoter.QuoteColumnName(column)}"
                        ).ToList();

                        output.AppendLine("WHEN MATCHED THEN");
                        output.AppendLine($"    UPDATE SET {string.Join(", ", updateAssignments)}");
                    }
                }

                // WHEN NOT MATCHED THEN INSERT
                output.AppendLine("WHEN NOT MATCHED THEN");
                output.AppendLine($"    INSERT ({string.Join(", ", quotedColumnNames)})");
                output.AppendLine($"    VALUES ({string.Join(", ", quotedColumnNames.Select(name => $"source.{name}"))})");
            }

            AppendSqlStatementEndToken(output);

            return output.ToString();
        }
    }
}
