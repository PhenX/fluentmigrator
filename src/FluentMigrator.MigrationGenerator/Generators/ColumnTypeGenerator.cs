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
using System.Globalization;

using DatabaseSchemaReader.DataSchema;

namespace FluentMigrator.MigrationGenerator.Generators
{
    /// <summary>
    /// Generates FluentMigrator column type method calls from database column definitions.
    /// </summary>
    public static class ColumnTypeGenerator
    {
        /// <summary>
        /// Gets the FluentMigrator method call for the column's data type.
        /// </summary>
        /// <param name="column">The database column.</param>
        /// <returns>A string representing the FluentMigrator method call.</returns>
        public static string GetColumnType(DatabaseColumn column)
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

        /// <summary>
        /// Escapes and formats a default value for use in FluentMigrator code.
        /// </summary>
        /// <param name="defaultValue">The default value string from the database.</param>
        /// <param name="dataType">The data type of the column.</param>
        /// <returns>A string representing the default value in C# code.</returns>
        public static string EscapeDefaultValue(string defaultValue, DataType dataType)
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
    }
}
