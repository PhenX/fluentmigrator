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
    /// Generates FluentMigrator constraint creation code (unique constraints and foreign keys).
    /// </summary>
    public static class ConstraintGenerator
    {
        /// <summary>
        /// Generates Create.UniqueConstraint() code for all unique constraints on a table.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateUniqueConstraints(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            foreach (var constraint in table.UniqueKeys.OrderBy(c => c.Name))
            {
                GenerateCreateUniqueConstraint(sb, table, constraint, indent);
            }
        }

        /// <summary>
        /// Generates Delete.UniqueConstraint() code for all unique constraints on a table (for Down() migration).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateDeleteUniqueConstraints(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            foreach (var constraint in table.UniqueKeys.OrderByDescending(c => c.Name))
            {
                GenerateDeleteUniqueConstraint(sb, table, constraint, indent);
            }
        }

        /// <summary>
        /// Generates Create.ForeignKey() code for all foreign keys on a table.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateForeignKeys(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            foreach (var fk in table.ForeignKeys.OrderBy(f => f.Name))
            {
                GenerateCreateForeignKey(sb, table, fk, indent);
            }
        }

        /// <summary>
        /// Generates Delete.ForeignKey() code for all foreign keys on a table (for Down() migration).
        /// </summary>
        /// <param name="sb">The StringBuilder to append the code to.</param>
        /// <param name="table">The database table.</param>
        /// <param name="indent">The indentation string.</param>
        public static void GenerateDeleteForeignKeys(StringBuilder sb, DatabaseTable table, string indent = "            ")
        {
            foreach (var fk in table.ForeignKeys.OrderByDescending(f => f.Name))
            {
                GenerateDeleteForeignKey(sb, table, fk, indent);
            }
        }

        /// <summary>
        /// Gets all foreign keys that reference the specified table.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="tableName">The name of the table being referenced.</param>
        /// <returns>A collection of foreign keys that reference the table.</returns>
        public static IEnumerable<(DatabaseTable Table, DatabaseConstraint ForeignKey)> GetForeignKeysReferencingTable(
            DatabaseSchema schema, string tableName)
        {
            return schema.Tables
                .SelectMany(t => t.ForeignKeys.Select(fk => (Table: t, ForeignKey: fk)))
                .Where(x => x.ForeignKey.RefersToTable == tableName)
                .OrderBy(x => x.Table.Name)
                .ThenBy(x => x.ForeignKey.Name);
        }

        private static void GenerateCreateUniqueConstraint(StringBuilder sb, DatabaseTable table, DatabaseConstraint constraint, string indent)
        {
            var line = new StringBuilder();
            line.Append($"{indent}Create.UniqueConstraint(\"{constraint.Name}\")");
            line.Append($".OnTable(\"{table.Name}\")");

            var columns = constraint.Columns;
            if (columns.Count == 1)
            {
                line.Append($".Column(\"{columns[0]}\")");
            }
            else
            {
                var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
                line.Append($".Columns({columnList})");
            }

            line.Append(';');
            sb.AppendLine(line.ToString());
        }

        private static void GenerateDeleteUniqueConstraint(StringBuilder sb, DatabaseTable table, DatabaseConstraint constraint, string indent)
        {
            sb.AppendLine($"{indent}Delete.UniqueConstraint(\"{constraint.Name}\").FromTable(\"{table.Name}\");");
        }

        private static void GenerateCreateForeignKey(StringBuilder sb, DatabaseTable table, DatabaseConstraint fk, string indent)
        {
            var line = new StringBuilder();

            // Generate a name if the FK doesn't have one (e.g., SQLite)
            var fkName = !string.IsNullOrEmpty(fk.Name)
                ? fk.Name
                : $"FK_{table.Name}_{fk.RefersToTable}_{string.Join("_", fk.Columns)}";

            line.Append($"{indent}Create.ForeignKey(\"{fkName}\")");
            line.Append($".FromTable(\"{table.Name}\")");

            var fromColumns = fk.Columns;
            if (fromColumns.Count == 1)
            {
                line.Append($".ForeignColumn(\"{fromColumns[0]}\")");
            }
            else
            {
                var columnList = string.Join(", ", fromColumns.Select(c => $"\"{c}\""));
                line.Append($".ForeignColumns({columnList})");
            }

            line.Append($".ToTable(\"{fk.RefersToTable}\")");

            var toColumns = fk.ReferencedColumns(table.DatabaseSchema).ToList();
            if (toColumns.Count == 1)
            {
                line.Append($".PrimaryColumn(\"{toColumns[0]}\")");
            }
            else
            {
                var columnList = string.Join(", ", toColumns.Select(c => $"\"{c}\""));
                line.Append($".PrimaryColumns({columnList})");
            }

            // Add ON DELETE and ON UPDATE rules if specified
            if (!string.IsNullOrEmpty(fk.DeleteRule) && fk.DeleteRule != "NO ACTION")
            {
                line.Append(GetRuleMethod("OnDelete", fk.DeleteRule));
            }

            if (!string.IsNullOrEmpty(fk.UpdateRule) && fk.UpdateRule != "NO ACTION")
            {
                line.Append(GetRuleMethod("OnUpdate", fk.UpdateRule));
            }

            line.Append(';');
            sb.AppendLine(line.ToString());
        }

        private static void GenerateDeleteForeignKey(StringBuilder sb, DatabaseTable table, DatabaseConstraint fk, string indent)
        {
            // Generate a name if the FK doesn't have one (e.g., SQLite)
            var fkName = !string.IsNullOrEmpty(fk.Name)
                ? fk.Name
                : $"FK_{table.Name}_{fk.RefersToTable}_{string.Join("_", fk.Columns)}";

            sb.AppendLine($"{indent}Delete.ForeignKey(\"{fkName}\").OnTable(\"{table.Name}\");");
        }

        private static string GetRuleMethod(string methodPrefix, string rule)
        {
            return rule?.ToUpperInvariant() switch
            {
                "CASCADE" => $".{methodPrefix}Cascade()",
                "SET NULL" => $".{methodPrefix}SetNull()",
                "SET DEFAULT" => $".{methodPrefix}SetDefault()",
                "RESTRICT" => $".{methodPrefix}Restrict()",
                _ => ""
            };
        }
    }
}
