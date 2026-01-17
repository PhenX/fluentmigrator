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

namespace FluentMigrator.MigrationGenerator
{
    /// <summary>
    /// Configuration options for the migration generator.
    /// </summary>
    public class MigrationGeneratorOptions
    {
        /// <summary>
        /// Gets or sets the connection string to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the database provider type (e.g., 'SqlServer', 'PostgreSql', 'MySql', 'SQLite', 'Oracle').
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the namespace for the generated migration classes.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the output directory where migration files will be generated.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the generation mode.
        /// </summary>
        public GenerationMode Mode { get; set; } = GenerationMode.SingleMigration;

        /// <summary>
        /// Gets or sets the database schema to read. If not specified, the default schema is used.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the list of table names to exclude from generation.
        /// </summary>
        public List<string> ExcludeTables { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of table names to include in generation.
        /// If specified, only these tables will be generated.
        /// </summary>
        public List<string> IncludeTables { get; set; } = new List<string>();
    }
}
