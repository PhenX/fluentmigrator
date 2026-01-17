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

        private IList<MigrationFile> GenerateSingleMigration(DatabaseSchema schema)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var className = "InitialMigration";
            var fileName = $"{timestamp}_{className}.cs";

            var filteredTables = FilterTables(schema.Tables);
            var content = GenerateMigrationClass(className, timestamp, filteredTables);

            return new List<MigrationFile>
            {
                new MigrationFile { FileName = fileName, Content = content }
            };
        }

        private IList<MigrationFile> GenerateOnePerTable(DatabaseSchema schema)
        {
            var files = new List<MigrationFile>();
            var baseTimestamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            var tables = FilterTables(schema.Tables).OrderBy(t => t.Name).ToList();
            for (int i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                var timestamp = (baseTimestamp + i).ToString(CultureInfo.InvariantCulture);
                var className = $"Create{SanitizeClassName(table.Name)}Table";
                var fileName = $"{timestamp}_{className}.cs";

                var content = GenerateMigrationClass(className, timestamp, new List<DatabaseTable> { table });

                files.Add(new MigrationFile { FileName = fileName, Content = content });
            }

            return files;
        }

        private string GenerateMigrationClass(string className, string timestamp, IEnumerable<DatabaseTable> tables)
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

            foreach (var table in tables)
            {
                GenerateCreateTable(sb, table);
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void Down()");
            sb.AppendLine("        {");

            foreach (var table in tables.Reverse())
            {
                sb.AppendLine($"            Delete.Table(\"{table.Name}\");");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateCreateTable(StringBuilder sb, DatabaseTable table)
        {
            sb.AppendLine($"            Create.Table(\"{table.Name}\")");

            var columns = table.Columns.OrderBy(c => c.IsPrimaryKey ? 0 : 1).ThenBy(c => c.Ordinal).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var isLast = i == columns.Count - 1;
                GenerateColumn(sb, column, isLast);
            }

            sb.AppendLine();
        }

        private void GenerateColumn(StringBuilder sb, DatabaseColumn column, bool isLast)
        {
            var columnLine = new StringBuilder();
            columnLine.Append($"                .WithColumn(\"{column.Name}\")");

            columnLine.Append(GetColumnType(column));

            if (column.IsPrimaryKey)
            {
                columnLine.Append(".PrimaryKey()");
            }

            if (column.IsAutoNumber)
            {
                columnLine.Append(".Identity()");
            }

            if (column.IsUniqueKey && !column.IsPrimaryKey)
            {
                columnLine.Append(".Unique()");
            }

            if (column.IsIndexed && !column.IsPrimaryKey && !column.IsUniqueKey)
            {
                columnLine.Append(".Indexed()");
            }

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
                var defaultValue = EscapeDefaultValue(column.DefaultValue, column.DataType);
                columnLine.Append($".WithDefaultValue({defaultValue})");
            }

            if (isLast)
            {
                columnLine.Append(';');
            }

            sb.AppendLine(columnLine.ToString());
        }

        private static string GetColumnType(DatabaseColumn column)
        {
            var dataType = column.DbDataType?.ToUpperInvariant() ?? column.DataType?.TypeName?.ToUpperInvariant() ?? "NVARCHAR";

            return dataType switch
            {
                "INT" or "INTEGER" or "INT4" => ".AsInt32()",
                "BIGINT" or "INT8" => ".AsInt64()",
                "SMALLINT" or "INT2" => ".AsInt16()",
                "TINYINT" => ".AsByte()",
                "BIT" or "BOOLEAN" or "BOOL" => ".AsBoolean()",
                "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY" => GetDecimalType(column),
                "FLOAT" or "REAL" or "DOUBLE" or "DOUBLE PRECISION" => ".AsDouble()",
                "DATE" => ".AsDate()",
                "TIME" => ".AsTime()",
                "DATETIME" or "DATETIME2" or "SMALLDATETIME" or "TIMESTAMP" => ".AsDateTime()",
                "DATETIMEOFFSET" or "TIMESTAMPTZ" => ".AsDateTimeOffset()",
                "UNIQUEIDENTIFIER" or "UUID" or "GUID" => ".AsGuid()",
                "BINARY" or "VARBINARY" or "IMAGE" or "BYTEA" => GetBinaryType(column),
                "TEXT" or "NTEXT" or "CLOB" or "LONGTEXT" or "MEDIUMTEXT" => ".AsString(int.MaxValue)",
                "CHAR" or "NCHAR" => GetStringType(column, true),
                "VARCHAR" or "NVARCHAR" or "VARCHAR2" or "NVARCHAR2" or "CHARACTER VARYING" => GetStringType(column, false),
                "XML" => ".AsXml()",
                _ => GetDefaultStringType(column)
            };
        }

        private static string GetDecimalType(DatabaseColumn column)
        {
            if (column.Precision.HasValue && column.Scale.HasValue)
            {
                return $".AsDecimal({column.Precision.Value}, {column.Scale.Value})";
            }
            return ".AsDecimal()";
        }

        private static string GetBinaryType(DatabaseColumn column)
        {
            if (column.Length.HasValue && column.Length.Value > 0 && column.Length.Value < int.MaxValue)
            {
                return $".AsBinary({column.Length.Value})";
            }
            return ".AsBinary()";
        }

        private static string GetStringType(DatabaseColumn column, bool isFixedLength)
        {
            var method = isFixedLength ? ".AsFixedLengthString" : ".AsString";
            if (column.Length.HasValue && column.Length.Value > 0 && column.Length.Value < int.MaxValue)
            {
                return $"{method}({column.Length.Value})";
            }
            return $"{method}()";
        }

        private static string GetDefaultStringType(DatabaseColumn column)
        {
            if (column.Length.HasValue && column.Length.Value > 0 && column.Length.Value < int.MaxValue)
            {
                return $".AsString({column.Length.Value})";
            }
            return ".AsString()";
        }

        private static string EscapeDefaultValue(string defaultValue, DataType dataType)
        {
            if (defaultValue is null)
            {
                return "null";
            }

            var value = defaultValue.Trim();

            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (value.StartsWith("'") && value.EndsWith("'"))
            {
                value = value.Substring(1, value.Length - 2);
                return $"\"{value.Replace("\"", "\\\"")}\"";
            }

            if (string.Equals(value, "GETDATE()", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "NOW()", StringComparison.OrdinalIgnoreCase))
            {
                return "SystemMethods.CurrentDateTime";
            }

            if (string.Equals(value, "GETUTCDATE()", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "UTC_TIMESTAMP()", StringComparison.OrdinalIgnoreCase))
            {
                return "SystemMethods.CurrentUTCDateTime";
            }

            if (string.Equals(value, "NEWID()", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "UUID()", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "GEN_RANDOM_UUID()", StringComparison.OrdinalIgnoreCase))
            {
                return "SystemMethods.NewGuid";
            }

            if (string.Equals(value, "NEWSEQUENTIALID()", StringComparison.OrdinalIgnoreCase))
            {
                return "SystemMethods.NewSequentialId";
            }

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue ? "true" : "false";
            }

            if (string.Equals(value, "1", StringComparison.Ordinal) && 
                dataType?.TypeName?.ToUpperInvariant() is "BIT" or "BOOLEAN" or "BOOL")
            {
                return "true";
            }

            if (string.Equals(value, "0", StringComparison.Ordinal) &&
                dataType?.TypeName?.ToUpperInvariant() is "BIT" or "BOOLEAN" or "BOOL")
            {
                return "false";
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\\\"")}\"";
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
