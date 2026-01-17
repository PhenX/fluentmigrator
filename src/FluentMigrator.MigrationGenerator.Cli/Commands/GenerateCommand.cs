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
using System.IO;
using System.Text.Json;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.Configuration;

namespace FluentMigrator.MigrationGenerator.Cli.Commands
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
        public string OutputPath { get; set; }

        [Option("-n|--namespace <NAMESPACE>", Description = "The namespace for the generated migration classes.")]
        public string Namespace { get; set; }

        [Option("-m|--mode <MODE>", Description = "Generation mode: 'SingleMigration' for all tables in one file, 'OnePerTable' for separate files per table.")]
        public GenerationMode? Mode { get; set; }

        [Option("-p|--provider <PROVIDER>", Description = "The database provider type (e.g., 'SqlServer', 'PostgreSql', 'MySql', 'SQLite', 'Oracle').")]
        public string Provider { get; set; }

        [Option("-s|--schema <SCHEMA>", Description = "The database schema to read. If not specified, the default schema is used.")]
        public string Schema { get; set; }

        [Option("--config <CONFIG_PATH>", Description = "Path to a JSON configuration file containing generation options.")]
        public string ConfigPath { get; set; }

        [Option("--exclude-tables <TABLES>", Description = "Comma-separated list of table names to exclude from generation.")]
        public string ExcludeTables { get; set; }

        [Option("--include-tables <TABLES>", Description = "Comma-separated list of table names to include in generation. If specified, only these tables will be generated.")]
        public string IncludeTables { get; set; }

        protected int OnExecute(IConsole console)
        {
            try
            {
                var options = BuildOptions(console);
                if (options is null)
                {
                    return 1;
                }

                if (string.IsNullOrEmpty(options.ConnectionString))
                {
                    console.Error.WriteLine("Error: A connection string must be provided either via --connection-string, --appsettings with --connection-key, or in the config file.");
                    return 1;
                }

                if (string.IsNullOrEmpty(options.Provider))
                {
                    console.Error.WriteLine("Error: A database provider must be specified via --provider or in the config file.");
                    return 1;
                }

                if (string.IsNullOrEmpty(options.Namespace))
                {
                    console.Error.WriteLine("Error: A namespace must be specified via --namespace or in the config file.");
                    return 1;
                }

                if (string.IsNullOrEmpty(options.OutputPath))
                {
                    console.Error.WriteLine("Error: An output path must be specified via --output or in the config file.");
                    return 1;
                }

                if (!Directory.Exists(options.OutputPath))
                {
                    Directory.CreateDirectory(options.OutputPath);
                }

                var generator = new MigrationCodeGenerator(options);
                var migrations = generator.Generate();

                foreach (var migration in migrations)
                {
                    var filePath = Path.Combine(options.OutputPath, migration.FileName);
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

        private MigrationGeneratorOptions BuildOptions(IConsole console)
        {
            MigrationGeneratorOptions options;

            // Load from config file if specified
            if (!string.IsNullOrEmpty(ConfigPath))
            {
                if (!File.Exists(ConfigPath))
                {
                    console.Error.WriteLine($"Error: The config file '{ConfigPath}' does not exist.");
                    return null;
                }

                var json = File.ReadAllText(ConfigPath);
                options = JsonSerializer.Deserialize<MigrationGeneratorOptions>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                }) ?? new MigrationGeneratorOptions();
            }
            else
            {
                options = new MigrationGeneratorOptions();
            }

            // Override with CLI arguments
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                options.ConnectionString = ConnectionString;
            }
            else if (!string.IsNullOrEmpty(AppSettingsPath) && !string.IsNullOrEmpty(ConnectionKey))
            {
                var resolvedConnectionString = ResolveConnectionString(console);
                if (!string.IsNullOrEmpty(resolvedConnectionString))
                {
                    options.ConnectionString = resolvedConnectionString;
                }
            }

            if (!string.IsNullOrEmpty(Provider))
            {
                options.Provider = Provider;
            }

            if (!string.IsNullOrEmpty(Namespace))
            {
                options.Namespace = Namespace;
            }

            if (!string.IsNullOrEmpty(OutputPath))
            {
                options.OutputPath = OutputPath;
            }

            if (Mode.HasValue)
            {
                options.Mode = Mode.Value;
            }

            if (!string.IsNullOrEmpty(Schema))
            {
                options.Schema = Schema;
            }

            if (!string.IsNullOrEmpty(ExcludeTables))
            {
                var tables = ExcludeTables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                options.ExcludeTables.AddRange(tables);
            }

            if (!string.IsNullOrEmpty(IncludeTables))
            {
                var tables = IncludeTables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                options.IncludeTables.AddRange(tables);
            }

            return options;
        }

        private string ResolveConnectionString(IConsole console)
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
    }
}
