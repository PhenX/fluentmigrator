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

using System.Linq;
using System.Text;

using DatabaseSchemaReader.DataSchema;

namespace FluentMigrator.MigrationGenerator.Generators
{
    /// <summary>
    /// Generates FluentMigrator table creation code from database table definitions.
    /// </summary>
    public static class TableGenerator
    {
        /// <summary>
        /// Generates the Create.Table() code for a database table.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateCreateTable(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            sb.AppendLine($"{indent}Create.Table(\"{table.Name}\")");

            var columns = table.Columns.OrderBy(c => c.IsPrimaryKey ? 0 : 1).ThenBy(c => c.Ordinal).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var isLast = i == columns.Count - 1;
                GenerateColumn(sb, column, isLast, indent);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Generates the Delete.Table() code for a database table.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateDeleteTable(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            sb.AppendLine($"{indent}Delete.Table(\"{table.Name}\");");
        }

        private static void GenerateColumn(StringBuilder sb, DatabaseColumn column, bool isLast, string indent)
        {
            var columnLine = new StringBuilder();
            columnLine.Append($"{indent}    .WithColumn(\"{column.Name}\")");

            columnLine.Append(ColumnTypeGenerator.GetColumnType(column));

            if (column.IsPrimaryKey)
            {
                columnLine.Append(".PrimaryKey()");
            }

            if (column.IsAutoNumber)
            {
                columnLine.Append(".Identity()");
            }

            // Note: We don't add Unique() here for single-column unique constraints
            // as they are handled by ConstraintGenerator for explicit unique constraints

            if (!column.IsPrimaryKey)
            {
                if (column.Nullable)
                {
                    columnLine.Append(".Nullable()");
                }
                else
                {
                    columnLine.Append(".NotNullable()");
                }
            }

            if (!string.IsNullOrEmpty(column.DefaultValue))
            {
                var defaultValue = ColumnTypeGenerator.EscapeDefaultValue(column.DefaultValue, column.DataType);
                columnLine.Append($".WithDefaultValue({defaultValue})");
            }

            if (isLast)
            {
                columnLine.Append(';');
            }

            sb.AppendLine(columnLine.ToString());
        }
    }
}
