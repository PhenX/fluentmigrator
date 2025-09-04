# Edge Cases and Troubleshooting

This guide covers common edge cases you might encounter when using FluentMigrator and how to handle them effectively.

## Common Issues

### Migration Version Conflicts

#### Problem: Duplicate Migration Versions
```csharp
[Migration(20240101120000)]
public class CreateUsersTable : Migration { /* ... */ }

[Migration(20240101120000)] // Same version!
public class CreateProductsTable : Migration { /* ... */ }
```

**Error**: FluentMigrator will throw an exception about duplicate migration versions.

**Solution**: Use unique version numbers, preferably timestamps:
```csharp
[Migration(20240101120000)]
public class CreateUsersTable : Migration { /* ... */ }

[Migration(20240101120100)] // Different timestamp
public class CreateProductsTable : Migration { /* ... */ }
```

#### Problem: Out-of-Order Migration Versions
```csharp
// Already applied: 20240101120000, 20240101130000
[Migration(20240101125000)] // This is between already applied migrations
public class LateAddition : Migration { /* ... */ }
```

**Solution**: FluentMigrator will still apply this migration, but it's better to use a newer timestamp:
```csharp
[Migration(20240101140000)] // Use a timestamp after the last applied migration
public class LateAddition : Migration { /* ... */ }
```

### Database Connection Issues

#### Connection String Problems
```csharp
// Common mistakes:
"Server=.;Database=MyApp;Trusted_Connection=true;Timeout=30;"           // Wrong: Timeout
"Server=.;Database=MyApp;Trusted_Connection=true;Connection Timeout=30;" // Correct
```

#### Timeout Issues During Long Migrations
```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSqlServer()
        .WithGlobalConnectionString(connectionString)
        .WithGlobalCommandTimeout(TimeSpan.FromMinutes(30))) // Increase timeout
    .Configure<ProcessorOptions>(options =>
    {
        options.Timeout = TimeSpan.FromMinutes(30);
    });
```

### Schema and Table Issues

#### Problem: Column Dependencies
```csharp
public override void Down()
{
    // This will fail if there are constraints referencing this column
    Delete.Column("UserId").FromTable("Orders");
}
```

**Solution**: Drop dependent constraints first:
```csharp
public override void Down()
{
    Delete.ForeignKey("FK_Orders_Users").OnTable("Orders");
    Delete.Column("UserId").FromTable("Orders");
}
```

#### Problem: Default Constraints (SQL Server)
```csharp
public override void Down()
{
    // This may fail if there's a default constraint
    Delete.Column("IsActive").FromTable("Users");
}
```

**Solution**: Drop the default constraint first:
```csharp
public override void Down()
{
    Delete.DefaultConstraint().OnTable("Users").OnColumn("IsActive");
    Delete.Column("IsActive").FromTable("Users");
}
```

### Data Migration Issues

#### Problem: Large Dataset Migrations
```csharp
// This can cause timeouts or memory issues
Execute.Sql("UPDATE Users SET Status = 'Active' WHERE Status IS NULL");
```

**Solution**: Use batched operations:
```csharp
[Migration(1)]
public class BatchedUpdate : Migration
{
    public override void Up()
    {
        // SQL Server batch update
        Execute.Sql(@"
            WHILE @@ROWCOUNT > 0
            BEGIN
                UPDATE TOP (1000) Users 
                SET Status = 'Active' 
                WHERE Status IS NULL
            END");
    }

    public override void Down()
    {
        Execute.Sql(@"
            WHILE @@ROWCOUNT > 0
            BEGIN
                UPDATE TOP (1000) Users 
                SET Status = NULL 
                WHERE Status = 'Active'
            END");
    }
}
```

#### Problem: Data Loss in Down Migrations
```csharp
public override void Up()
{
    Alter.Table("Users").AddColumn("NewField").AsString(100).Nullable();
    Execute.Sql("UPDATE Users SET NewField = 'Default Value'");
}

public override void Down()
{
    // This loses all data in NewField!
    Delete.Column("NewField").FromTable("Users");
}
```

**Solution**: Consider data preservation strategies:
```csharp
public override void Down()
{
    // Option 1: Warn about data loss
    Execute.Sql("-- WARNING: This will lose data in NewField column");
    Delete.Column("NewField").FromTable("Users");

    // Option 2: Create backup table (if feasible)
    // Execute.Sql("SELECT * INTO Users_NewField_Backup FROM Users WHERE NewField IS NOT NULL");
    // Delete.Column("NewField").FromTable("Users");
}
```

### Cross-Database Compatibility Issues

#### Problem: Database-Specific SQL in Migrations
```csharp
public override void Up()
{
    Execute.Sql("SELECT TOP 10 * FROM Users"); // SQL Server specific
}
```

**Solution**: Use conditional logic:
```csharp
public override void Up()
{
    IfDatabase("SqlServer")
        .Execute.Sql("SELECT TOP 10 * FROM Users");
        
    IfDatabase("Postgres")
        .Execute.Sql("SELECT * FROM Users LIMIT 10");
        
    IfDatabase("MySql")
        .Execute.Sql("SELECT * FROM Users LIMIT 10");
}
```

#### Problem: Data Type Incompatibilities
```csharp
// This won't work consistently across databases
Create.Table("Events")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
    .WithColumn("OccurredAt").AsDateTime2().NotNullable(); // DateTime2 is SQL Server specific
```

**Solution**: Use portable data types or conditional logic:
```csharp
Create.Table("Events")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
    .WithColumn("OccurredAt").AsDateTime().NotNullable(); // More portable

// Or use conditional logic for precision
IfDatabase("SqlServer")
    .Alter.Table("Events")
    .AlterColumn("OccurredAt").AsDateTime2();
```

### Transaction and Concurrency Issues

#### Problem: Migrations in Transactions
Some database operations can't be performed within transactions:
```csharp
// This might fail in some databases
public override void Up()
{
    Execute.Sql("CREATE INDEX CONCURRENTLY IX_Users_Email ON Users (Email)"); // PostgreSQL
}
```

**Solution**: Check if your database supports the operation within transactions:
```csharp
public override void Up()
{
    IfDatabase("Postgres")
        .Execute.Sql("CREATE INDEX CONCURRENTLY IX_Users_Email ON Users (Email)");
        
    IfDatabase("SqlServer")
        .Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email");
}
```

#### Problem: Deadlocks During Migration
In production environments, migrations might conflict with application queries.

**Solution**: Consider maintenance windows and use appropriate isolation levels:
```csharp
[Migration(1)]
public class MaintenanceMigration : Migration
{
    public override void Up()
    {
        // For SQL Server, consider setting snapshot isolation
        Execute.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON");
        Execute.Sql("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON");
        
        // Perform migration operations
        Alter.Table("Users").AddColumn("NewColumn").AsString(100).Nullable();
    }

    public override void Down()
    {
        Delete.Column("NewColumn").FromTable("Users");
    }
}
```

## Advanced Edge Cases

### Circular Dependencies

#### Problem: Foreign Key Circular References
```csharp
// This creates a circular dependency
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("ManagerId").AsInt32().Nullable()
        .ForeignKey("FK_Users_Manager", "Users", "Id");

Create.Table("Departments")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("ManagerId").AsInt32().NotNullable()
        .ForeignKey("FK_Departments_Users", "Users", "Id");

Alter.Table("Users")
    .AddColumn("DepartmentId").AsInt32().Nullable()
        .ForeignKey("FK_Users_Departments", "Departments", "Id");
```

**Solution**: Create tables without foreign keys first, then add constraints:
```csharp
public override void Up()
{
    // Create tables without foreign keys
    Create.Table("Users")
        .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
        .WithColumn("ManagerId").AsInt32().Nullable()
        .WithColumn("DepartmentId").AsInt32().Nullable();

    Create.Table("Departments")
        .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
        .WithColumn("ManagerId").AsInt32().NotNullable();

    // Add foreign keys after tables exist
    Create.ForeignKey("FK_Users_Manager")
        .FromTable("Users").ForeignColumn("ManagerId")
        .ToTable("Users").PrimaryColumn("Id");

    Create.ForeignKey("FK_Departments_Users")
        .FromTable("Departments").ForeignColumn("ManagerId")
        .ToTable("Users").PrimaryColumn("Id");

    Create.ForeignKey("FK_Users_Departments")
        .FromTable("Users").ForeignColumn("DepartmentId")
        .ToTable("Departments").PrimaryColumn("Id");
}
```

### Identity Column Edge Cases

#### Problem: Identity Insert Issues
```csharp
public override void Up()
{
    Create.Table("Users")
        .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
        .WithColumn("Username").AsString(50).NotNullable();

    // This will fail because of IDENTITY column
    Insert.IntoTable("Users").Row(new { Id = 1, Username = "admin" });
}
```

**Solution**: Handle identity inserts properly:
```csharp
public override void Up()
{
    Create.Table("Users")
        .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
        .WithColumn("Username").AsString(50).NotNullable();

    IfDatabase("SqlServer").Execute.Sql("SET IDENTITY_INSERT Users ON");
    Insert.IntoTable("Users").Row(new { Id = 1, Username = "admin" });
    IfDatabase("SqlServer").Execute.Sql("SET IDENTITY_INSERT Users OFF");
}
```

### Schema Evolution Edge Cases

#### Problem: Renaming Tables with Dependencies
```csharp
public override void Up()
{
    // This might fail if there are foreign keys referencing this table
    Rename.Table("Users").To("People");
}
```

**Solution**: Handle dependencies explicitly:
```csharp
public override void Up()
{
    // Find and recreate foreign keys
    var foreignKeys = GetForeignKeysReferencingTable("Users");
    
    foreach (var fk in foreignKeys)
    {
        Delete.ForeignKey(fk.Name).OnTable(fk.Table);
    }
    
    Rename.Table("Users").To("People");
    
    foreach (var fk in foreignKeys)
    {
        Create.ForeignKey(fk.Name)
            .FromTable(fk.Table).ForeignColumn(fk.Column)
            .ToTable("People").PrimaryColumn(fk.ReferencedColumn);
    }
}

private List<ForeignKeyInfo> GetForeignKeysReferencingTable(string tableName)
{
    // Implementation depends on your database
    // This is a simplified example
    return new List<ForeignKeyInfo>();
}
```

## Testing Edge Cases

### Integration Testing for Edge Cases
```csharp
[Test]
public void Migration_ShouldHandleCircularDependencies()
{
    // Test that migrations with circular dependencies work correctly
    using (var scope = ServiceProvider.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        
        // Should not throw
        Assert.DoesNotThrow(() => runner.MigrateUp(migrationVersion));
        
        // Verify all tables and constraints exist
        Assert.IsTrue(TableExists("Users"));
        Assert.IsTrue(TableExists("Departments"));
        Assert.IsTrue(ForeignKeyExists("FK_Users_Manager"));
    }
}

[Test]
public void Migration_ShouldHandleLargeDataSets()
{
    // Test performance with large datasets
    CreateLargeDataSet(100000); // Create 100k records
    
    var stopwatch = Stopwatch.StartNew();
    
    using (var scope = ServiceProvider.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp(migrationVersion);
    }
    
    stopwatch.Stop();
    Assert.Less(stopwatch.ElapsedMilliseconds, 300000); // Should complete in 5 minutes
}
```

### Rollback Testing
```csharp
[Test]
public void Migration_ShouldRollbackCleanly()
{
    using (var scope = ServiceProvider.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        
        // Migrate up
        runner.MigrateUp(migrationVersion);
        Assert.IsTrue(TableExists("Users"));
        
        // Migrate down
        runner.MigrateDown(migrationVersion);
        Assert.IsFalse(TableExists("Users"));
    }
}
```

## Best Practices for Edge Cases

### 1. Defensive Programming
```csharp
public override void Up()
{
    // Check if table exists before creating
    IfDatabase("SqlServer").Execute.Sql(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
        BEGIN
            -- Table creation logic here
        END
    ");
}
```

### 2. Comprehensive Down Methods
```csharp
public override void Down()
{
    // Always implement Down methods, even if they just document the inability to rollback
    Execute.Sql("-- WARNING: This migration cannot be fully rolled back due to data loss");
    Execute.Sql("-- Manual intervention may be required");
    
    // Attempt partial rollback
    Delete.Column("NewColumn").FromTable("Users");
}
```

### 3. Documentation and Logging
```csharp
[Migration(20240101120000)]
/// <summary>
/// Adds user preferences table with JSON data.
/// WARNING: This migration modifies existing user data and cannot be fully rolled back.
/// Edge cases handled:
/// - Large user datasets (uses batched operations)
/// - Cross-database JSON compatibility (conditional logic)
/// - Default constraint cleanup on rollback
/// </summary>
public class AddUserPreferences : Migration
{
    public override void Up()
    {
        // Log the start of migration
        Execute.Sql("-- Starting AddUserPreferences migration");
        
        // Migration logic here
        
        Execute.Sql("-- Completed AddUserPreferences migration");
    }
    
    public override void Down()
    {
        Execute.Sql("-- Starting rollback of AddUserPreferences migration");
        Execute.Sql("-- WARNING: This will lose user preference data");
        
        // Rollback logic here
    }
}
```

## Monitoring and Alerting

### Production Migration Monitoring
```csharp
[Migration(1)]
public class ProductionSafeMigration : Migration
{
    public override void Up()
    {
        // Log migration start
        Execute.Sql("INSERT INTO MigrationLog (Migration, Status, StartTime) VALUES ('ProductionSafeMigration', 'Started', GETDATE())");
        
        try
        {
            // Migration operations
            Create.Table("NewTable")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity();
                
            // Log success
            Execute.Sql("UPDATE MigrationLog SET Status = 'Completed', EndTime = GETDATE() WHERE Migration = 'ProductionSafeMigration'");
        }
        catch
        {
            // Log failure
            Execute.Sql("UPDATE MigrationLog SET Status = 'Failed', EndTime = GETDATE() WHERE Migration = 'ProductionSafeMigration'");
            throw;
        }
    }
    
    public override void Down()
    {
        Delete.Table("NewTable");
        Execute.Sql("INSERT INTO MigrationLog (Migration, Status, StartTime, EndTime) VALUES ('ProductionSafeMigration', 'Rolled Back', GETDATE(), GETDATE())");
    }
}
```

## Recovery Strategies

### Manual Recovery Scripts
When migrations fail, have recovery scripts ready:

```sql
-- recovery_script.sql
-- Recovery script for failed migration 20240101120000

-- Step 1: Check current state
SELECT * FROM VersionInfo WHERE Version = 20240101120000;

-- Step 2: Manual cleanup if needed
-- DROP TABLE IF EXISTS PartiallyCreatedTable;

-- Step 3: Reset migration state
-- DELETE FROM VersionInfo WHERE Version = 20240101120000;

-- Step 4: Verify database is in expected state
-- Add verification queries here
```

This comprehensive guide should help you handle most edge cases you'll encounter with FluentMigrator. Remember that prevention is better than cure - thorough testing in non-production environments is crucial.