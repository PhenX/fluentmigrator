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
using System.ComponentModel.DataAnnotations;
using System.Linq;

using FluentMigrator.Infrastructure;
using FluentMigrator.Model;

namespace FluentMigrator.Expressions
{
    /// <summary>
    /// Expression to upsert (insert or update) data using SQL MERGE or equivalent
    /// </summary>
    public class UpsertDataExpression : IMigrationExpression, ISupportAdditionalFeatures, ISchemaExpression, IValidatableObject
    {
        /// <inheritdoc />
        public string SchemaName { get; set; }

        /// <summary>
        /// Gets or sets the table name
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.TableNameCannotBeNullOrEmpty))]
        public string TableName { get; set; }

        /// <summary>
        /// Gets the columns to match on for determining if a row exists (merge keys)
        /// </summary>
        public List<string> MatchColumns { get; } = new List<string>();

        /// <summary>
        /// Gets the rows to be upserted
        /// </summary>
        public List<InsertionDataDefinition> Rows { get; } = new List<InsertionDataDefinition>();

        /// <summary>
        /// Gets or sets the columns to update when a match is found (if null, all non-match columns are updated)
        /// </summary>
        public List<string> UpdateColumns { get; set; }

        /// <inheritdoc />
        public IDictionary<string, object> AdditionalFeatures { get; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public void ExecuteWith(IMigrationProcessor processor)
        {
            processor.Process(this);
        }

        /// <inheritdoc />
        public IMigrationExpression Reverse()
        {
            // For reversal, we create a delete expression that removes the inserted rows
            var expression = new DeleteDataExpression
            {
                SchemaName = SchemaName,
                TableName = TableName
            };

            // Create delete conditions based on the match columns
            for (var index = Rows.Count - 1; index >= 0; index--)
            {
                var dataDefinition = new DeletionDataDefinition();
                var row = Rows[index];

                // Only include match columns in the delete condition
                foreach (var matchColumn in MatchColumns)
                {
                    var matchingPair = row.FirstOrDefault(kvp => kvp.Key == matchColumn);
                    if (!matchingPair.Equals(default(KeyValuePair<string, object>)))
                    {
                        dataDefinition.Add(matchingPair);
                    }
                }

                if (dataDefinition.Count > 0)
                {
                    expression.Rows.Add(dataDefinition);
                }
            }

            return expression;
        }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MatchColumns == null || MatchColumns.Count == 0)
                yield return new ValidationResult("UpsertDataExpression must specify at least one match column");

            if (Rows == null || Rows.Count == 0)
                yield return new ValidationResult("UpsertDataExpression must specify at least one row to upsert");

            // Validate that all rows contain the match columns
            foreach (var row in Rows ?? Enumerable.Empty<InsertionDataDefinition>())
            {
                foreach (var matchColumn in MatchColumns ?? Enumerable.Empty<string>())
                {
                    if (!row.Any(kvp => kvp.Key == matchColumn))
                        yield return new ValidationResult($"Row data must contain all match columns. Missing column: {matchColumn}");
                }
            }

            // Validate that UpdateColumns (if specified) don't include match columns
            if (UpdateColumns != null)
            {
                var invalidUpdateColumns = UpdateColumns.Intersect(MatchColumns ?? Enumerable.Empty<string>()).ToList();
                if (invalidUpdateColumns.Count > 0)
                    yield return new ValidationResult($"Update columns cannot include match columns: {string.Join(", ", invalidUpdateColumns)}");
            }
        }
    }
}