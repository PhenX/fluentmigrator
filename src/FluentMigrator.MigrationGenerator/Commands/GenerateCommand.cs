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
using System.ComponentModel.DataAnnotations;
using System.IO;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.Configuration;

namespace FluentMigrator.MigrationGenerator.Commands
{
    [HelpOption(Description = "Generate FluentMigrator migration classes from an existing database")]
    [Command("dotnet-fm-generator", Description = "Generates FluentMigrator migration classes from an existing database schema using DatabaseSchemaReader")]
    public class GenerateCommand
    {
        [Option("-c|--connection-string <CONNECTION_STRING>", Description = "The connection string to the database.")]
        public string ConnectionString { get; set; }

        [Option("-a|--appsettings <APPSETTINGS_PATH>", Description = "Path to the appsettings.json file containing the connection string.")]
        public string AppSettingsPath { get; set; }

        [Option("-k|--connection-key <CONNECTION_KEY>", Description = "The key of the connection string in the appsettings.json file (e.g., 'DefaultConnection').")]
        public string ConnectionKey { get; set; }

        [Option("-o|--output <OUTPUT_PATH>", Description = "The output directory where migration files will be generated.")]
        [Required]
        public string OutputPath { get; set; }

        [Option("-n|--namespace <NAMESPACE>", Description = "The namespace for the generated migration classes.")]
        [Required]
        public string Namespace { get; set; }

        [Option("-m|--mode <MODE>", Description = "Generation mode: 'SingleMigration' for all tables in one file, 'OnePerTable' for separate files per table.")]
        public GenerationMode Mode { get; set; } = GenerationMode.SingleMigration;

        [Option("-p|--provider <PROVIDER>", Description = "The database provider type (e.g., 'SqlServer', 'PostgreSql', 'MySql', 'SQLite', 'Oracle').")]
        [Required]
        public string Provider { get; set; }

        [Option("-s|--schema <SCHEMA>", Description = "The database schema to read. If not specified, the default schema is used.")]
        public string Schema { get; set; }

        protected int OnExecute(IConsole console)
        {
            try
            {
                var resolvedConnectionString = ResolveConnectionString(console);
                if (string.IsNullOrEmpty(resolvedConnectionString))
                {
                    console.Error.WriteLine("Error: A connection string must be provided either via --connection-string or via --appsettings and --connection-key.");
                    return 1;
                }

                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }

                var generator = new MigrationCodeGenerator(resolvedConnectionString, Provider, Namespace, Schema);
                var migrations = generator.Generate(Mode);

                foreach (var migration in migrations)
                {
                    var filePath = Path.Combine(OutputPath, migration.FileName);
                    File.WriteAllText(filePath, migration.Content);
                    console.WriteLine($"Generated: {filePath}");
                }

                console.WriteLine($"Successfully generated {migrations.Count} migration file(s).");
                return 0;
            }
            catch (Exception ex)
            {
                console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private string ResolveConnectionString(IConsole console)
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                return ConnectionString;
            }

            if (!string.IsNullOrEmpty(AppSettingsPath) && !string.IsNullOrEmpty(ConnectionKey))
            {
                if (!File.Exists(AppSettingsPath))
                {
                    console.Error.WriteLine($"Error: The appsettings file '{AppSettingsPath}' does not exist.");
                    return null;
                }

                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(AppSettingsPath, optional: false)
                    .Build();

                var connectionString = configuration.GetConnectionString(ConnectionKey);
                if (string.IsNullOrEmpty(connectionString))
                {
                    console.Error.WriteLine($"Error: Connection string key '{ConnectionKey}' not found in '{AppSettingsPath}'.");
                    return null;
                }

                return connectionString;
            }

            return null;
        }
    }
}
