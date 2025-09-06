# Migration Base

The `Migration` base class is the foundation of all FluentMigrator migrations. This guide covers the core API, methods, and patterns for creating effective migrations.

## Migration Class Structure

### Basic Migration Template

```csharp
[Migration(202401151000)]
public class ExampleMigration : Migration
{
    public override void Up()
    {
        // Forward migration logic goes here
    }

    public override void Down()
    {
        // Rollback migration logic goes here  
    }
}
```

### Migration Attributes

```csharp
// Basic migration with version number
[Migration(202401151000)]
public class BasicMigration : Migration { }

// Migration with description
[Migration(202401151100, "Add user email verification system")]
public class DescriptiveMigration : Migration { }

// Migration with transaction behavior
[Migration(202401151200, TransactionBehavior.None)]
public class NonTransactionalMigration : Migration { }

// Migration with breaking change flag
[Migration(202401151300, BreakingChange = true)]
public class BreakingChangeMigration : Migration { }

// Migration targeting specific database
[Migration(202401151400, "Add PostgreSQL specific indexes")]
[Tags("Postgres")]
public class DatabaseSpecificMigration : Migration { }
```

## Core Migration Methods

### Abstract Methods (Must Override)

```csharp
public class CoreMethodsExample : Migration
{
    /// <summary>
    /// Implement forward migration logic
    /// This method is called when migrating up
    /// </summary>
    public override void Up()
    {
        // Schema changes, data migrations, etc.
        Create.Table("NewTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
    }

    /// <summary>
    /// Implement rollback migration logic
    /// This method is called when rolling back
    /// </summary>
    public override void Down()
    {
        // Reverse the changes made in Up()
        Delete.Table("NewTable");
    }
}
```

### Optional Virtual Methods

```csharp
public class VirtualMethodsExample : Migration
{
    /// <summary>
    /// Called before Up() method execution
    /// Use for validation, logging, or setup
    /// </summary>
    public override void GetUpExpressions(IMigrationContext context)
    {
        Console.WriteLine($"Starting migration {GetType().Name}");
        
        // Perform pre-migration validation
        ValidatePreconditions();
        
        // Call base implementation to execute Up()
        base.GetUpExpressions(context);
        
        Console.WriteLine($"Completed migration {GetType().Name}");
    }

    /// <summary>
    /// Called before Down() method execution
    /// Use for validation, logging, or cleanup setup
    /// </summary>
    public override void GetDownExpressions(IMigrationContext context)
    {
        Console.WriteLine($"Starting rollback of {GetType().Name}");
        
        // Perform pre-rollback validation
        ValidateRollbackPreconditions();
        
        // Call base implementation to execute Down()
        base.GetDownExpressions(context);
        
        Console.WriteLine($"Completed rollback of {GetType().Name}");
    }
    
    public override void Up()
    {
        Create.Table("ExampleTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("Data").AsString(500).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ExampleTable");
    }
    
    private void ValidatePreconditions()
    {
        // Example validation
        if (!Schema.Table("Users").Exists())
        {
            throw new InvalidOperationException("Users table must exist before running this migration");
        }
    }
    
    private void ValidateRollbackPreconditions()
    {
        // Example rollback validation
        var recordCount = Execute.Sql("SELECT COUNT(*) FROM ExampleTable").Returns<int>().FirstOrDefault();
        if (recordCount > 0)
        {
            Console.WriteLine($"Warning: Rolling back table with {recordCount} records");
        }
    }
}
```

## Migration Context and Metadata

### Accessing Migration Context

```csharp
[Migration(202401151500)]
public class MigrationContextExample : Migration
{
    public override void GetUpExpressions(IMigrationContext context)
    {
        // Access connection string
        var connectionString = context.Connection?.ConnectionString;
        Console.WriteLine($"Migrating database: {ExtractDatabaseName(connectionString)}");
        
        // Access migration metadata
        var migrationMetadata = context.MigrationAssembly
            .GetMigrationMetadata(202401151500);
        
        if (migrationMetadata != null)
        {
            Console.WriteLine($"Migration Description: {migrationMetadata.Description}");
            Console.WriteLine($"Breaking Change: {migrationMetadata.HasBreakingChange}");
        }
        
        // Access query schema
        var querySchema = context.QuerySchema;
        if (querySchema.DatabaseType == "SqlServer")
        {
            Console.WriteLine("Applying SQL Server optimizations");
        }
        
        base.GetUpExpressions(context);
    }
    
    public override void Up()
    {
        Create.Table("ContextExample")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("MigrationVersion").AsInt64().NotNullable()
            .WithColumn("AppliedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Insert metadata about this migration
        var version = GetType().GetCustomAttribute<MigrationAttribute>()?.Version ?? 0;
        Insert.IntoTable("ContextExample")
            .Row(new { MigrationVersion = version });
    }
    
    private string ExtractDatabaseName(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return "Unknown";
        
        // Simple extraction - in real scenarios, use connection string builder
        var dbMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)");
        return dbMatch.Success ? dbMatch.Groups[1].Value : "Unknown";
    }

    public override void Down()
    {
        Delete.Table("ContextExample");
    }
}
```

### Custom Migration Metadata

```csharp
/// <summary>
/// Custom migration with extended metadata
/// </summary>
[Migration(202401151600, "Add advanced user management system")]
[Tags("UserManagement", "Security")]
[MaintenanceMode(false)]
[EstimatedDuration(minutes: 5)]
[Author("John Doe", "john.doe@company.com")]
[RequiredBy("v2.1.0")]
public class CustomMetadataMigration : Migration
{
    public override void Up()
    {
        var metadata = GetMigrationMetadata();
        LogMigrationStart(metadata);
        
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform migration
            Create.Table("UserManagementSystem")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Username").AsString(50).NotNullable()
                .WithColumn("PasswordHash").AsString(255).NotNullable()
                .WithColumn("SecurityLevel").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("LastLoginAt").AsDateTime().Nullable()
                .WithColumn("FailedLoginAttempts").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("AccountLockedUntil").AsDateTime().Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
                .WithColumn("CreatedBy").AsString(100).NotNullable().WithDefaultValue("System");
                
            Create.Index("UQ_UserManagementSystem_Username")
                .OnTable("UserManagementSystem")
                .OnColumn("Username")
                .Unique();
                
            stopwatch.Stop();
            LogMigrationComplete(metadata, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            LogMigrationError(metadata, ex);
            throw;
        }
    }
    
    private MigrationMetadata GetMigrationMetadata()
    {
        var migrationAttr = GetType().GetCustomAttribute<MigrationAttribute>();
        var authorAttr = GetType().GetCustomAttribute<AuthorAttribute>();
        var durationAttr = GetType().GetCustomAttribute<EstimatedDurationAttribute>();
        
        return new MigrationMetadata
        {
            Version = migrationAttr?.Version ?? 0,
            Description = migrationAttr?.Description ?? "",
            Author = authorAttr?.Name ?? "Unknown",
            AuthorEmail = authorAttr?.Email ?? "",
            EstimatedDuration = durationAttr?.Duration ?? TimeSpan.Zero
        };
    }
    
    private void LogMigrationStart(MigrationMetadata metadata)
    {
        Console.WriteLine($"Starting migration: {metadata.Description}");
        Console.WriteLine($"Author: {metadata.Author} <{metadata.AuthorEmail}>");
        Console.WriteLine($"Estimated duration: {metadata.EstimatedDuration.TotalMinutes} minutes");
    }
    
    private void LogMigrationComplete(MigrationMetadata metadata, TimeSpan actualDuration)
    {
        Console.WriteLine($"Migration completed: {metadata.Description}");
        Console.WriteLine($"Actual duration: {actualDuration.TotalMinutes:F1} minutes");
        
        if (actualDuration > metadata.EstimatedDuration.Multiply(1.5))
        {
            Console.WriteLine("WARNING: Migration took significantly longer than estimated");
        }
    }
    
    private void LogMigrationError(MigrationMetadata metadata, Exception ex)
    {
        Console.WriteLine($"Migration failed: {metadata.Description}");
        Console.WriteLine($"Error: {ex.Message}");
    }

    public override void Down()
    {
        Delete.Table("UserManagementSystem");
    }
}

// Custom attributes for enhanced metadata
[AttributeUsage(AttributeTargets.Class)]
public class MaintenanceModeAttribute : Attribute
{
    public bool RequiresMaintenanceMode { get; }
    
    public MaintenanceModeAttribute(bool requiresMaintenanceMode)
    {
        RequiresMaintenanceMode = requiresMaintenanceMode;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EstimatedDurationAttribute : Attribute
{
    public TimeSpan Duration { get; }
    
    public EstimatedDurationAttribute(int minutes)
    {
        Duration = TimeSpan.FromMinutes(minutes);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class AuthorAttribute : Attribute
{
    public string Name { get; }
    public string Email { get; }
    
    public AuthorAttribute(string name, string email = "")
    {
        Name = name;
        Email = email;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class RequiredByAttribute : Attribute
{
    public string Version { get; }
    
    public RequiredByAttribute(string version)
    {
        Version = version;
    }
}

public class MigrationMetadata
{
    public long Version { get; set; }
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
    public TimeSpan EstimatedDuration { get; set; }
}
```

## Migration Base Properties and Methods

### Protected Properties

```csharp
public class MigrationPropertiesExample : Migration
{
    public override void Up()
    {
        // Access the current application context
        var applicationContext = ApplicationContext;
        Console.WriteLine($"Application context: {applicationContext}");
        
        // Access connection info through the runner context
        LogConnectionInfo();
        
        // Access migration assembly information
        LogAssemblyInfo();
        
        Create.Table("PropertiesExample")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("ApplicationContext").AsString(200).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }
    
    private void LogConnectionInfo()
    {
        // Connection information is typically accessed through context
        // This is a conceptual example of what you might want to log
        Console.WriteLine("Migration connection context information:");
        Console.WriteLine($"  Database Provider: {GetDatabaseProvider()}");
        Console.WriteLine($"  Migration Direction: Up");
        Console.WriteLine($"  Transaction Support: Available");
    }
    
    private void LogAssemblyInfo()
    {
        var assembly = GetType().Assembly;
        Console.WriteLine($"Migration assembly: {assembly.GetName().Name}");
        Console.WriteLine($"Assembly version: {assembly.GetName().Version}");
        
        // Count total migrations in assembly
        var migrationCount = assembly.GetTypes()
            .Count(t => t.IsSubclassOf(typeof(Migration)));
        Console.WriteLine($"Total migrations in assembly: {migrationCount}");
    }
    
    private void HandleDatabaseSpecificLogic()
    {
        // Use IfDatabase with Delegate for conditional logic
        IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() => 
        {
            // SQL Server specific operations
            Execute.Sql("-- SQL Server specific logic");
        });
        
        IfDatabase(ProcessorIdConstants.Postgres).Delegate(() => 
        {
            // PostgreSQL specific operations  
            Execute.Sql("-- PostgreSQL specific logic");
        });
        
        IfDatabase(ProcessorIdConstants.MySql).Delegate(() => 
        {
            // MySQL specific operations
            Execute.Sql("-- MySQL specific logic");
        });
        
        IfDatabase(ProcessorIdConstants.SQLite).Delegate(() => 
        {
            // SQLite specific operations
            Execute.Sql("-- SQLite specific logic");
        });
        
        IfDatabase(ProcessorIdConstants.Oracle).Delegate(() => 
        {
            // Oracle specific operations
            Execute.Sql("-- Oracle specific logic");
        });
    }

    public override void Down()
    {
        Delete.Table("PropertiesExample");
    }
}
```

### Helper Methods

```csharp
public class MigrationHelperMethods : Migration
{
    public override void Up()
    {
        // Use built-in helper methods for common patterns
        CreateAuditColumns("Users");
        CreateLookupTable("UserStatus", new[] { "Active", "Inactive", "Pending", "Suspended" });
        AddTimestampColumns("Orders");
        CreateVersionedTable("Products");
    }
    
    /// <summary>
    /// Helper method to add standard audit columns to any table
    /// </summary>
    private void CreateAuditColumns(string tableName)
    {
        if (!Schema.Table(tableName).Exists())
        {
            Console.WriteLine($"Warning: Table {tableName} does not exist, skipping audit columns");
            return;
        }
        
        var columnsToAdd = new[]
        {
            new { Name = "CreatedBy", Type = "AsString", Size = 100, Nullable = false, Default = "System" },
            new { Name = "CreatedAt", Type = "AsDateTime", Size = 0, Nullable = false, Default = "CURRENT_TIMESTAMP" },
            new { Name = "ModifiedBy", Type = "AsString", Size = 100, Nullable = true, Default = (string)null },
            new { Name = "ModifiedAt", Type = "AsDateTime", Size = 0, Nullable = true, Default = (string)null }
        };
        
        foreach (var column in columnsToAdd)
        {
            if (!Schema.Table(tableName).Column(column.Name).Exists())
            {
                switch (column.Type)
                {
                    case "AsString":
                        if (column.Nullable)
                        {
                            Alter.Table(tableName).AddColumn(column.Name).AsString(column.Size).Nullable();
                        }
                        else
                        {
                            Alter.Table(tableName).AddColumn(column.Name).AsString(column.Size).NotNullable()
                                .WithDefaultValue(column.Default);
                        }
                        break;
                        
                    case "AsDateTime":
                        if (column.Nullable)
                        {
                            Alter.Table(tableName).AddColumn(column.Name).AsDateTime().Nullable();
                        }
                        else
                        {
                            Alter.Table(tableName).AddColumn(column.Name).AsDateTime().NotNullable()
                                .WithDefaultValue(SystemMethods.CurrentDateTime);
                        }
                        break;
                }
            }
        }
        
        Console.WriteLine($"Added audit columns to {tableName}");
    }
    
    /// <summary>
    /// Helper method to create standardized lookup tables
    /// </summary>
    private void CreateLookupTable(string tableName, string[] values)
    {
        if (Schema.Table(tableName).Exists())
        {
            Console.WriteLine($"Lookup table {tableName} already exists");
            return;
        }
        
        Create.Table(tableName)
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Code").AsString(50).NotNullable()
            .WithColumn("Description").AsString(200).NotNullable()
            .WithColumn("SortOrder").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Index($"UQ_{tableName}_Code")
            .OnTable(tableName)
            .OnColumn("Code")
            .Unique();
            
        // Insert lookup values
        for (int i = 0; i < values.Length; i++)
        {
            Insert.IntoTable(tableName)
                .Row(new
                {
                    Code = values[i],
                    Description = values[i],
                    SortOrder = (i + 1) * 10, // Leave gaps for insertions
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
        }
        
        Console.WriteLine($"Created lookup table {tableName} with {values.Length} values");
    }
    
    /// <summary>
    /// Helper method to add standard timestamp columns
    /// </summary>
    private void AddTimestampColumns(string tableName)
    {
        if (!Schema.Table(tableName).Exists())
        {
            Console.WriteLine($"Warning: Table {tableName} does not exist, skipping timestamp columns");
            return;
        }
        
        if (!Schema.Table(tableName).Column("CreatedAt").Exists())
        {
            Alter.Table(tableName)
                .AddColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
        }
        
        if (!Schema.Table(tableName).Column("UpdatedAt").Exists())
        {
            Alter.Table(tableName)
                .AddColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
        }
        
        // Create trigger for UpdatedAt (database specific)
        CreateUpdateTrigger(tableName);
        
        Console.WriteLine($"Added timestamp columns to {tableName}");
    }
    
    /// <summary>
    /// Helper method to create versioned table with history tracking
    /// </summary>
    private void CreateVersionedTable(string tableName)
    {
        if (Schema.Table(tableName).Exists())
        {
            Console.WriteLine($"Versioned table {tableName} already exists");
            return;
        }
        
        // Create main table
        Create.Table(tableName)
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("IsCurrentVersion").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("CreatedBy").AsString(100).NotNullable().WithDefaultValue("System");
            
        // Create history table
        Create.Table($"{tableName}History")
            .WithColumn("HistoryId").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OriginalId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("Version").AsInt32().NotNullable()
            .WithColumn("ChangeType").AsString(20).NotNullable() // INSERT, UPDATE, DELETE
            .WithColumn("ChangedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("ChangedBy").AsString(100).NotNullable();
            
        // Create indexes
        Create.Index($"IX_{tableName}_Version")
            .OnTable(tableName)
            .OnColumn("Version");
            
        Create.Index($"IX_{tableName}History_OriginalId")
            .OnTable($"{tableName}History")
            .OnColumn("OriginalId");
            
        Console.WriteLine($"Created versioned table {tableName} with history tracking");
    }
    
    private void CreateUpdateTrigger(string tableName)
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Execute.Sql($@"
                CREATE TRIGGER TR_{tableName}_UpdatedAt
                ON {tableName}
                AFTER UPDATE
                AS
                BEGIN
                    UPDATE {tableName}
                    SET UpdatedAt = GETDATE()
                    FROM {tableName} t
                    INNER JOIN inserted i ON t.Id = i.Id
                END");
    });
    IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Execute.Sql($@"
                CREATE OR REPLACE FUNCTION update_{tableName.ToLower()}_updated_at()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.UpdatedAt = NOW();
                    RETURN NEW;
                END;
                $$ language 'plpgsql';
                
                CREATE TRIGGER TR_{tableName}_UpdatedAt
                    BEFORE UPDATE ON {tableName}
                    FOR EACH ROW
                    EXECUTE FUNCTION update_{tableName.ToLower()}_updated_at();");
    });
    IfDatabase(ProcessorIdConstants.MySql).Delegate(() =>
    {
Execute.Sql($@"
                CREATE TRIGGER TR_{tableName}_UpdatedAt
                BEFORE UPDATE ON {tableName}
                FOR EACH ROW
                SET NEW.UpdatedAt = NOW()");
    });
    }

    public override void Down()
    {
        // Clean up in reverse order
        Execute.Sql("DROP TABLE IF EXISTS ProductsHistory");
        Delete.Table("Products");
        Delete.Table("UserStatus");
        
        // Remove audit columns from Users table
        if (Schema.Table("Users").Exists())
        {
            var auditColumns = new[] { "CreatedBy", "CreatedAt", "ModifiedBy", "ModifiedAt" };
            foreach (var column in auditColumns)
            {
                if (Schema.Table("Users").Column(column).Exists())
                {
                    Delete.Column(column).FromTable("Users");
                }
            }
        }
        
        // Remove timestamp columns and triggers
        if (Schema.Table("Orders").Exists())
        {
            Execute.Sql("DROP TRIGGER IF EXISTS TR_Orders_UpdatedAt");
            Delete.Column("UpdatedAt").FromTable("Orders");
            Delete.Column("CreatedAt").FromTable("Orders");
        }
    }
}
```

## Error Handling and Validation

### Exception Handling in Migrations

```csharp
[Migration(202401151800)]
public class ErrorHandlingMigration : Migration
{
    public override void Up()
    {
        try
        {
            ValidateEnvironment();
            PerformMigration();
            ValidateResults();
        }
        catch (MigrationValidationException ex)
        {
            Console.WriteLine($"Migration validation failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed with unexpected error: {ex.Message}");
            LogErrorDetails(ex);
            throw new MigrationException($"Migration {GetType().Name} failed", ex);
        }
    }
    
    private void ValidateEnvironment()
    {
        // Check database connectivity
        try
        {
            var result = Execute.Sql("SELECT 1").Returns<int>().FirstOrDefault();
            if (result != 1)
            {
                throw new MigrationValidationException("Database connectivity test failed");
            }
        }
        catch (Exception ex)
        {
            throw new MigrationValidationException("Cannot connect to database", ex);
        }
        
        // Check required tables exist
        var requiredTables = new[] { "Users", "Roles" };
        foreach (var table in requiredTables)
        {
            if (!Schema.Table(table).Exists())
            {
                throw new MigrationValidationException($"Required table '{table}' does not exist");
            }
        }
        
        // Check disk space (conceptual - implementation would be database-specific)
        if (!HasSufficientDiskSpace())
        {
            throw new MigrationValidationException("Insufficient disk space for migration");
        }
    }
    
    private void PerformMigration()
    {
        Console.WriteLine("Performing migration with error handling...");
        
        // Wrap each operation in try-catch for granular error handling
        try
        {
            Create.Table("ErrorHandlingExample")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Data").AsString(500).NotNullable()
                .WithColumn("Status").AsString(50).NotNullable().WithDefaultValue("Active")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
        }
        catch (Exception ex)
        {
            throw new MigrationException("Failed to create ErrorHandlingExample table", ex);
        }
        
        try
        {
            Create.Index("IX_ErrorHandlingExample_Status")
                .OnTable("ErrorHandlingExample")
                .OnColumn("Status");
        }
        catch (Exception ex)
        {
            // Index creation failure might not be critical - log and continue
            Console.WriteLine($"Warning: Failed to create index: {ex.Message}");
        }
        
        try
        {
            // Insert sample data with error handling
            InsertSampleDataSafely();
        }
        catch (Exception ex)
        {
            throw new MigrationException("Failed to insert sample data", ex);
        }
    }
    
    private void InsertSampleDataSafely()
    {
        var sampleData = new[]
        {
            new { Data = "Sample 1", Status = "Active" },
            new { Data = "Sample 2", Status = "Inactive" },
            new { Data = "Sample 3", Status = "Pending" }
        };
        
        foreach (var item in sampleData)
        {
            try
            {
                Insert.IntoTable("ErrorHandlingExample").Row(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to insert sample data: {ex.Message}");
                // Continue with other records
            }
        }
    }
    
    private void ValidateResults()
    {
        // Verify table was created
        if (!Schema.Table("ErrorHandlingExample").Exists())
        {
            throw new MigrationValidationException("ErrorHandlingExample table was not created successfully");
        }
        
        // Verify data was inserted
        var recordCount = Execute.Sql("SELECT COUNT(*) FROM ErrorHandlingExample")
            .Returns<int>().FirstOrDefault();
            
        if (recordCount == 0)
        {
            Console.WriteLine("Warning: No sample data was inserted");
        }
        else
        {
            Console.WriteLine($"Successfully inserted {recordCount} sample records");
        }
        
        // Verify index exists
        if (!IndexExists("ErrorHandlingExample", "IX_ErrorHandlingExample_Status"))
        {
            Console.WriteLine("Warning: Status index was not created");
        }
    }
    
    private bool HasSufficientDiskSpace()
    {
        // This would be database-specific implementation
        // For example, checking available space on SQL Server
        try
        {
                IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                    SELECT 
                        SUM(size - FILEPROPERTY(name, 'SpaceUsed')) * 8 / 1024 AS FreeSpaceMB
                    FROM sys.database_files 
                    WHERE type = 0");
        }
        catch
        {
            // If we can't check, assume we have space
        }
        
        return true;
    }
    
    private bool IndexExists(string tableName, string indexName)
    {
        try
        {
                IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
var exists = Execute.Sql($@"
                    SELECT COUNT(*)
                    FROM sys.indexes i
                    INNER JOIN sys.objects o ON i.object_id = o.object_id
                    WHERE o.name = '{tableName}' AND i.name = '{indexName}'")
                    .Returns<int>().FirstOrDefault();
                    
                return exists > 0;
    });
        }
        catch
        {
            // If we can't check, assume it doesn't exist
        }
        
        return false;
    }
    
    private void LogErrorDetails(Exception ex)
    {
        Console.WriteLine($"Error Type: {ex.GetType().Name}");
        Console.WriteLine($"Error Message: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
    }

    public override void Down()
    {
        try
        {
            if (Schema.Table("ErrorHandlingExample").Exists())
            {
                Delete.Table("ErrorHandlingExample");
                Console.WriteLine("Successfully rolled back ErrorHandlingExample table");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during rollback: {ex.Message}");
            throw;
        }
    }
}

// Custom exception classes for migration-specific errors
public class MigrationValidationException : Exception
{
    public MigrationValidationException(string message) : base(message) { }
    public MigrationValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public class MigrationException : Exception
{
    public MigrationException(string message) : base(message) { }
    public MigrationException(string message, Exception innerException) : base(message, innerException) { }
}
```

## Advanced Migration Base Patterns

### Abstract Base Migration Classes

```csharp
/// <summary>
/// Base class for migrations that require audit logging
/// </summary>
public abstract class AuditedMigration : Migration
{
    protected abstract string MigrationName { get; }
    protected abstract string MigrationDescription { get; }
    
    public sealed override void GetUpExpressions(IMigrationContext context)
    {
        LogMigrationStart();
        
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            base.GetUpExpressions(context);
            
            stopwatch.Stop();
            LogMigrationSuccess(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            LogMigrationFailure(ex);
            throw;
        }
    }
    
    public sealed override void GetDownExpressions(IMigrationContext context)
    {
        LogRollbackStart();
        
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            base.GetDownExpressions(context);
            
            stopwatch.Stop();
            LogRollbackSuccess(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            LogRollbackFailure(ex);
            throw;
        }
    }
    
    private void LogMigrationStart()
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Starting migration: {MigrationName}");
        Console.WriteLine($"Description: {MigrationDescription}");
        
        // Log to audit table if it exists
        if (Schema.Table("MigrationAuditLog").Exists())
        {
            Insert.IntoTable("MigrationAuditLog")
                .Row(new
                {
                    MigrationName = MigrationName,
                    Action = "START",
                    Description = MigrationDescription,
                    StartedAt = DateTime.UtcNow,
                    Status = "RUNNING"
                });
        }
    }
    
    private void LogMigrationSuccess(TimeSpan duration)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Migration completed: {MigrationName}");
        Console.WriteLine($"Duration: {duration.TotalSeconds:F1} seconds");
        
        if (Schema.Table("MigrationAuditLog").Exists())
        {
            Execute.Sql($@"
                UPDATE MigrationAuditLog 
                SET Status = 'SUCCESS', 
                    CompletedAt = '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}',
                    Duration = {duration.TotalSeconds}
                WHERE MigrationName = '{MigrationName}' AND Action = 'START' AND Status = 'RUNNING'");
        }
    }
    
    private void LogMigrationFailure(Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Migration failed: {MigrationName}");
        Console.WriteLine($"Error: {ex.Message}");
        
        if (Schema.Table("MigrationAuditLog").Exists())
        {
            Execute.Sql($@"
                UPDATE MigrationAuditLog 
                SET Status = 'FAILED', 
                    CompletedAt = '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}',
                    ErrorMessage = '{ex.Message.Replace("'", "''")}'
                WHERE MigrationName = '{MigrationName}' AND Action = 'START' AND Status = 'RUNNING'");
        }
    }
    
    private void LogRollbackStart()
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Starting rollback: {MigrationName}");
    }
    
    private void LogRollbackSuccess(TimeSpan duration)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Rollback completed: {MigrationName}");
        Console.WriteLine($"Duration: {duration.TotalSeconds:F1} seconds");
    }
    
    private void LogRollbackFailure(Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Rollback failed: {MigrationName}");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

/// <summary>
/// Example usage of audited migration base class
/// </summary>
[Migration(202401151900)]
public class ExampleAuditedMigration : AuditedMigration
{
    protected override string MigrationName => "Add Customer Support System";
    protected override string MigrationDescription => "Creates tables for customer support ticketing system";
    
    public override void Up()
    {
        Create.Table("SupportTickets")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("Priority").AsString(20).NotNullable().WithDefaultValue("Medium")
            .WithColumn("Status").AsString(50).NotNullable().WithDefaultValue("Open")
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("AssignedToUserId").AsInt32().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }
    
    public override void Down()
    {
        Delete.Table("SupportTickets");
    }
}
```

## See Also

- [Builders](builders.md) - FluentMigrator builder patterns and syntax
- [Expressions](expressions.md) - Understanding migration expressions
- [Best Practices](../advanced/best-practices.md)
- [Common Operations](../operations/)
- [Examples](../examples/)