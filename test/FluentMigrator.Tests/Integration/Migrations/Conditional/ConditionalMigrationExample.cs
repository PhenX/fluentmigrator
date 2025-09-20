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

using FluentMigrator;

namespace FluentMigrator.Tests.Integration.Migrations.Conditional
{
    /// <summary>
    /// Example migration demonstrating conditional schema-based migrations using expression trees
    /// This migration conditionally deletes data from a table only if certain schema conditions are met
    /// </summary>
    [Migration(20241220001)]
    public class ConditionalMigrationExample : Migration
    {
        public override void Up()
        {
            // Create some tables for testing conditional logic
            Create.Table("VersionInfo")
                .WithColumn("Version").AsInt64().NotNullable().PrimaryKey()
                .WithColumn("AppliedOn").AsDateTime().Nullable()
                .WithColumn("Description").AsString().Nullable();

            Create.Table("AuditLog")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Action").AsString(50).NotNullable()
                .WithColumn("Timestamp").AsDateTime().NotNullable();

            // Example 1: Simple table existence check
            If(_ => Schema.Table("VersionInfo").Exists())
                .Then(_ =>
                {
                    Insert.IntoTable("VersionInfo").Row(new { Version = 1, AppliedOn = System.DateTime.Now, Description = "Initial version" });
                });

            // Example 2: OR condition - Execute if either table exists
            If(_ => Schema.Table("VersionInfo").Exists() || Schema.Table("AuditLog").Exists())
                .Then(_ =>
                {
                    Insert.IntoTable("AuditLog").Row(new { Action = "Migration executed", Timestamp = System.DateTime.Now });
                });

            // Example 3: AND condition - Only execute if both conditions are true
            If(_ => Schema.Table("VersionInfo").Exists() && Schema.Table("AuditLog").Exists())
                .Then(_ =>
                {
                    Execute.Sql("UPDATE VersionInfo SET Description = 'Updated by conditional migration' WHERE Version = 1");
                });

            // Example 4: Schema existence check
            If(_ => Schema.Schema("dbo").Exists())
                .Then(_ =>
                {
                    Create.Index("IX_VersionInfo_Version").OnTable("VersionInfo").OnColumn("Version");
                });

            // Example 5: Complex conditional logic for cleanup
            If(_ => Schema.Table("VersionInfo").Exists())
                .Then(_ =>
                {
                    // This would only run if VersionInfo table exists
                    Delete.FromTable("VersionInfo").Row(new { Version = 0 }); // Clean up any version 0 records
                });
        }

        public override void Down()
        {
            // Clean up in reverse order
            If(_ => Schema.Table("AuditLog").Exists())
                .Then(_ =>
                {
                    Delete.Table("AuditLog");
                });

            If(_ => Schema.Table("VersionInfo").Exists())
                .Then(_ =>
                {
                    Delete.Table("VersionInfo");
                });
        }
    }
}