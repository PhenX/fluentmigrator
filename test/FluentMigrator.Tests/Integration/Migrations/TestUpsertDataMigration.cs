using System;

namespace FluentMigrator.Tests.Integration.Migrations
{
    /// <summary>
    /// Integration test migration for UPSERT functionality
    /// </summary>
    [Migration(9999001)] // Using a high number to avoid conflicts
    public class TestUpsertDataMigration : Migration
    {
        public override void Up()
        {
            // Create test table for upsert operations
            Create.Table("UpsertTestTable")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Email").AsString(255).NotNullable().Unique()
                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Category").AsString(50).Nullable()
                .WithColumn("LastModified").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

            // Insert initial test data
            Insert.IntoTable("UpsertTestTable").Row(new 
            { 
                Email = "existing@example.com", 
                Name = "Existing User", 
                IsActive = true,
                Category = "Test"
            });

            // Test basic upsert - should update existing record
            Upsert.IntoTable("UpsertTestTable")
                .MatchOn("Email")
                .Row(new 
                { 
                    Email = "existing@example.com", 
                    Name = "Updated Existing User", 
                    IsActive = false,
                    Category = "Updated"
                });

            // Test upsert with new record - should insert new record
            Upsert.IntoTable("UpsertTestTable")
                .MatchOn("Email")
                .Row(new 
                { 
                    Email = "new@example.com", 
                    Name = "New User", 
                    IsActive = true,
                    Category = "New"
                });

            // Test upsert with multiple match columns
            Upsert.IntoTable("UpsertTestTable")
                .MatchOn("Email", "Category")
                .Row(new 
                { 
                    Email = "multikey@example.com", 
                    Name = "Multi Key User", 
                    IsActive = true,
                    Category = "MultiKey"
                });

            // Test upsert with specific update columns
            Upsert.IntoTable("UpsertTestTable")
                .MatchOn("Email")
                .Row(new 
                { 
                    Email = "selective@example.com", 
                    Name = "Selective Update User", 
                    IsActive = true,
                    Category = "Selective"
                })
                .UpdateColumns("Name"); // Only update Name on match

            // Test multiple row upsert
            Upsert.IntoTable("UpsertTestTable")
                .MatchOn("Email")
                .Rows(
                    new { Email = "bulk1@example.com", Name = "Bulk User 1", IsActive = true, Category = "Bulk" },
                    new { Email = "bulk2@example.com", Name = "Bulk User 2", IsActive = true, Category = "Bulk" },
                    new { Email = "bulk3@example.com", Name = "Bulk User 3", IsActive = false, Category = "Bulk" }
                );
        }

        public override void Down()
        {
            Delete.Table("UpsertTestTable");
        }
    }
}