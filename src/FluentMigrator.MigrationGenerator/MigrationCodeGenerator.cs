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
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using DatabaseSchemaReader;
using DatabaseSchemaReader.DataSchema;

using FluentMigrator.MigrationGenerator.Generators;

namespace FluentMigrator.MigrationGenerator
{
    /// <summary>
    /// Generates FluentMigrator migration classes from an existing database schema.
    /// </summary>
    public class MigrationCodeGenerator
    {
        private readonly MigrationGeneratorOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationCodeGenerator"/> class.
        /// </summary>
        /// <param name="options">The migration generator options.</param>
        public MigrationCodeGenerator(MigrationGeneratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(_options.ConnectionString))
                throw new ArgumentException("ConnectionString is required", nameof(options));
            if (string.IsNullOrEmpty(_options.Provider))
                throw new ArgumentException("Provider is required", nameof(options));
            if (string.IsNullOrEmpty(_options.Namespace))
                throw new ArgumentException("Namespace is required", nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationCodeGenerator"/> class
        /// for use with a pre-loaded schema (useful for testing).
        /// </summary>
        /// <param name="options">The migration generator options.</param>
        /// <param name="schema">The pre-loaded database schema.</param>
        public MigrationCodeGenerator(MigrationGeneratorOptions options, DatabaseSchema schema)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(_options.Namespace))
                throw new ArgumentException("Namespace is required", nameof(options));
        }

        /// <summary>
        /// Generates migration files based on the options.
        /// </summary>
        /// <returns>A list of generated migration files.</returns>
        public IList<MigrationFile> Generate()
        {
            using var connection = CreateConnection(_options.Provider, _options.ConnectionString);
            connection.Open();

            using var reader = new DatabaseReader(connection);

            if (!string.IsNullOrEmpty(_options.Schema))
            {
                reader.Owner = _options.Schema;
            }

            var schema = reader.ReadAll();

            return GenerateFromSchema(schema);
        }

        /// <summary>
        /// Generates migration files from a pre-loaded database schema.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <returns>A list of generated migration files.</returns>
        public IList<MigrationFile> GenerateFromSchema(DatabaseSchema schema)
        {
            return _options.Mode switch
            {
                GenerationMode.SingleMigration => GenerateSingleMigration(schema),
                GenerationMode.OnePerTable => GenerateOnePerTable(schema),
                _ => throw new ArgumentOutOfRangeException(nameof(_options.Mode), _options.Mode, "Unknown generation mode")
            };
        }

        private static DbConnection CreateConnection(string provider, string connectionString)
        {
            return provider?.ToLowerInvariant() switch
            {
                "sqlserver" or "mssql" or "sql" => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
                "postgresql" or "postgres" or "npgsql" => new Npgsql.NpgsqlConnection(connectionString),
                "mysql" or "mariadb" => new MySqlConnector.MySqlConnection(connectionString),
                "sqlite" => new Microsoft.Data.Sqlite.SqliteConnection(connectionString),
                "oracle" => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unknown database provider: {provider}. Supported providers: SqlServer, PostgreSql, MySql, SQLite, Oracle", nameof(provider))
            };
        }

        private static readonly HashSet<string> SystemTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sqlite_sequence", "sqlite_stat1", "sqlite_stat2", "sqlite_stat3", "sqlite_stat4",
            "sysdiagrams", "dtproperties",
            "__EFMigrationsHistory", "__MigrationHistory", "VersionInfo"
        };

        private IEnumerable<DatabaseTable> FilterTables(IEnumerable<DatabaseTable> tables)
        {
            var filtered = tables.Where(t => !SystemTableNames.Contains(t.Name) &&
                                     !t.Name.StartsWith("pg_", StringComparison.OrdinalIgnoreCase) &&
                                     !t.Name.StartsWith("sql_", StringComparison.OrdinalIgnoreCase) &&
                                     (!t.Name.StartsWith("sys", StringComparison.OrdinalIgnoreCase) ||
                                      t.Name.Equals("system", StringComparison.OrdinalIgnoreCase)));

            // Apply include filter if specified
            if (_options.IncludeTables?.Count > 0)
            {
                var includeSet = new HashSet<string>(_options.IncludeTables, StringComparer.OrdinalIgnoreCase);
                filtered = filtered.Where(t => includeSet.Contains(t.Name));
            }

            // Apply exclude filter
            if (_options.ExcludeTables?.Count > 0)
            {
                var excludeSet = new HashSet<string>(_options.ExcludeTables, StringComparer.OrdinalIgnoreCase);
                filtered = filtered.Where(t => !excludeSet.Contains(t.Name));
            }

            return filtered;
        }

        /// <summary>
        /// Orders tables for creation based on foreign key dependencies.
        /// Tables with no foreign keys come first, then tables that reference them, etc.
        /// </summary>
        private static IList<DatabaseTable> OrderTablesForCreation(IEnumerable<DatabaseTable> tables)
        {
            var tableList = tables.ToList();
            var result = new List<DatabaseTable>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tableDict = tableList.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            // Iteratively add tables whose dependencies are already processed
            while (result.Count < tableList.Count)
            {
                var added = false;
                foreach (var table in tableList.Where(t => !processed.Contains(t.Name)))
                {
                    var dependencies = table.ForeignKeys
                        .Select(fk => fk.RefersToTable)
                        .Where(refTable => tableDict.ContainsKey(refTable) && refTable != table.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    if (dependencies.All(d => processed.Contains(d)))
                    {
                        result.Add(table);
                        processed.Add(table.Name);
                        added = true;
                    }
                }

                // If no tables were added but we still have unprocessed tables,
                // there's a circular dependency - just add remaining tables
                if (!added)
                {
                    foreach (var table in tableList.Where(t => !processed.Contains(t.Name)))
                    {
                        result.Add(table);
                        processed.Add(table.Name);
                    }
                }
            }

            return result;
        }

        private IList<MigrationFile> GenerateSingleMigration(DatabaseSchema schema)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var className = "InitialMigration";
            var fileName = $"{timestamp}_{className}.cs";

            var filteredTables = FilterTables(schema.Tables).ToList();
            var orderedTables = OrderTablesForCreation(filteredTables);
            var content = GenerateMigrationClass(className, timestamp, orderedTables, schema);

            return new List<MigrationFile>
            {
                new MigrationFile { FileName = fileName, Content = content }
            };
        }

        private IList<MigrationFile> GenerateOnePerTable(DatabaseSchema schema)
        {
            var files = new List<MigrationFile>();
            var baseTimestamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            var filteredTables = FilterTables(schema.Tables).ToList();
            var orderedTables = OrderTablesForCreation(filteredTables);

            for (int i = 0; i < orderedTables.Count; i++)
            {
                var table = orderedTables[i];
                var timestamp = (baseTimestamp + i).ToString(CultureInfo.InvariantCulture);
                var className = $"Create{SanitizeClassName(table.Name)}Table";
                var fileName = $"{timestamp}_{className}.cs";

                var content = GenerateMigrationClass(className, timestamp, new List<DatabaseTable> { table }, schema);

                files.Add(new MigrationFile { FileName = fileName, Content = content });
            }

            return files;
        }

        private string GenerateMigrationClass(string className, string timestamp, IList<DatabaseTable> tables, DatabaseSchema schema)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using FluentMigrator;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_options.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    [Migration({timestamp})]");
            sb.AppendLine($"    public class {className} : Migration");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void Up()");
            sb.AppendLine("        {");

            // Order of operations in Up():
            // 1. Create tables (without foreign keys, just columns and primary keys)
            foreach (var table in tables)
            {
                TableGenerator.GenerateCreateTable(sb, table);
            }

            // 2. Create indexes
            foreach (var table in tables)
            {
                IndexGenerator.GenerateIndexes(sb, table);
            }

            // 3. Create unique constraints
            foreach (var table in tables)
            {
                ConstraintGenerator.GenerateUniqueConstraints(sb, table);
            }

            // 4. Create foreign keys (after all tables exist)
            foreach (var table in tables)
            {
                ConstraintGenerator.GenerateForeignKeys(sb, table);
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void Down()");
            sb.AppendLine("        {");

            // Order of operations in Down() - reverse of Up():
            // 1. Delete foreign keys first
            foreach (var table in tables.Reverse())
            {
                ConstraintGenerator.GenerateDeleteForeignKeys(sb, table);
            }

            // 2. Delete unique constraints
            foreach (var table in tables.Reverse())
            {
                ConstraintGenerator.GenerateDeleteUniqueConstraints(sb, table);
            }

            // 3. Delete indexes
            foreach (var table in tables.Reverse())
            {
                IndexGenerator.GenerateDeleteIndexes(sb, table);
            }

            // 4. Delete tables
            foreach (var table in tables.Reverse())
            {
                TableGenerator.GenerateDeleteTable(sb, table);
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string SanitizeClassName(string name)
        {
            var result = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result.Insert(0, '_');
            }

            return result.ToString();
        }
    }
}
