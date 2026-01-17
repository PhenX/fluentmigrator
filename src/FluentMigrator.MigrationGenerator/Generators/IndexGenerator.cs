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

using System.Collections.Generic;
using System.Linq;
using System.Text;

using DatabaseSchemaReader.DataSchema;

namespace FluentMigrator.MigrationGenerator.Generators
{
    /// <summary>
    /// Generates FluentMigrator index creation code from database index definitions.
    /// </summary>
    public static class IndexGenerator
    {
        /// <summary>
        /// Generates Create.Index() code for all indexes on a table.
        /// Excludes primary key indexes as they are handled by the primary key constraint.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateIndexes(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            var indexes = GetNonPrimaryKeyIndexes(table);

            foreach (var index in indexes)
            {
                GenerateCreateIndex(sb, table, index, indent);
            }
        }

        /// <summary>
        /// Generates Delete.Index() code for all indexes on a table (for Down() migration).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateDeleteIndexes(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            var indexes = GetNonPrimaryKeyIndexes(table).Reverse();

            foreach (var index in indexes)
            {
                GenerateDeleteIndex(sb, table, index, indent);
            }
        }

        /// <summary>
        /// Gets all indexes for a table, excluding primary key indexes and unique constraint indexes.
        /// </summary>
        private static IEnumerable<DatabaseIndex> GetNonPrimaryKeyIndexes(DatabaseTable table)
        {
            // Get constraint names to exclude (PK and unique constraints are handled separately)
            var constraintNames = new HashSet<string>();

            if (table.PrimaryKey != null)
            {
                constraintNames.Add(table.PrimaryKey.Name);
            }

            foreach (var uc in table.UniqueKeys)
            {
                constraintNames.Add(uc.Name);
            }

            return table.Indexes
                .Where(i => !i.IsUniqueKeyIndex(table) && !constraintNames.Contains(i.Name))
                .OrderBy(i => i.Name);
        }

        private static void GenerateCreateIndex(StringBuilder sb, DatabaseTable table, DatabaseIndex index, string indent)
        {
            var indexLine = new StringBuilder();
            indexLine.Append($"{indent}Create.Index(\"{index.Name}\")");
            indexLine.Append($".OnTable(\"{table.Name}\")");

            foreach (var column in index.Columns)
            {
                indexLine.Append($".OnColumn(\"{column.Name}\")");
                indexLine.Append(".Ascending()");
            }

            if (index.IsUnique)
            {
                indexLine.Append(".WithOptions().Unique()");
            }

            indexLine.Append(';');
            sb.AppendLine(indexLine.ToString());
        }

        private static void GenerateDeleteIndex(StringBuilder sb, DatabaseTable table, DatabaseIndex index, string indent)
        {
            sb.AppendLine($"{indent}Delete.Index(\"{index.Name}\").OnTable(\"{table.Name}\");");
        }
    }
}
