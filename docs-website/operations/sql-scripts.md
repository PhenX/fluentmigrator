# SQL Scripts

FluentMigrator provides the Execute.Sql family of methods for running custom SQL when the fluent API doesn't cover your specific needs. This guide covers all Execute.Sql methods and patterns for executing custom SQL scripts.

## Execute.Sql Methods

### Basic SQL Execution

```csharp
// Execute a simple SQL statement
Execute.Sql("UPDATE Users SET IsActive = 1 WHERE CreatedAt < '2023-01-01'");

// Execute with parameter-like syntax (still basic string)
Execute.Sql("DELETE FROM TempData WHERE ProcessedAt < DATEADD(day, -7, GETDATE())");
```

### Multi-Statement SQL Blocks

```csharp
Execute.Sql(@"
    CREATE TABLE #TempResults (
        Id INT,
        TotalCount INT
    );

    INSERT INTO #TempResults
    SELECT UserId, COUNT(*)
    FROM Orders
    GROUP BY UserId;

    UPDATE Users
    SET OrderCount = t.TotalCount
    FROM Users u
    INNER JOIN #TempResults t ON u.Id = t.Id;

    DROP TABLE #TempResults;
");
```

### Conditional SQL Execution

```csharp
public override void Up()
{
    IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
        UPDATE Users
        SET LastLogin = GETUTCDATE()
        WHERE Email LIKE '%@company.com'");

    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
        UPDATE Users
        SET LastLogin = NOW()
        WHERE Email LIKE '%@company.com'");

    IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
        UPDATE Users
        SET LastLogin = UTC_TIMESTAMP()
        WHERE Email LIKE '%@company.com'");
}
```

## Data Querying with Returns

While FluentMigrator is primarily for schema changes, sometimes you need to query data during migrations:

```csharp
[Migration(1)]
public class MigrateUserData : Migration
{
    public override void Up()
    {
        // Check if migration is needed
        Execute.Sql(@"
            IF EXISTS (SELECT 1 FROM Users WHERE Status IS NULL)
            BEGIN
                UPDATE Users
                SET Status = 'Active'
                WHERE Status IS NULL AND IsActive = 1

                UPDATE Users
                SET Status = 'Inactive'
                WHERE Status IS NULL AND IsActive = 0
            END
        ");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET Status = NULL WHERE Status IN ('Active', 'Inactive')");
    }
}
```

### Retrieving Data for Migration Decisions

```csharp
[Migration(2)]
public class ConditionalDataMigration : Migration
{
    public override void Up()
    {
        // Use conditional logic based on existing data
        Execute.Sql(@"
            DECLARE @UserCount INT
            SELECT @UserCount = COUNT(*) FROM Users

            IF @UserCount > 10000
            BEGIN
                -- Large dataset: use batch processing
                WHILE @@ROWCOUNT > 0
                BEGIN
                    UPDATE TOP (1000) Users
                    SET UpdatedAt = GETDATE()
                    WHERE UpdatedAt IS NULL
                END
            END
            ELSE
            BEGIN
                -- Small dataset: simple update
                UPDATE Users SET UpdatedAt = GETDATE() WHERE UpdatedAt IS NULL
            END
        ");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET UpdatedAt = NULL");
    }
}
```

## Advanced Execute.Sql Patterns

### Complex Migration Logic

```csharp
[Migration(3)]
public class ComplexDataMigration : Migration
{
    public override void Up()
    {
        // Use batch processing for large datasets
        IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
        {
            Execute.Sql(@"
                DECLARE @BatchSize INT = 5000;
                WHILE EXISTS (SELECT 1 FROM Users WHERE UpdatedAt IS NULL)
                BEGIN
                    UPDATE TOP (@BatchSize) Users
                    SET UpdatedAt = GETDATE()
                    WHERE UpdatedAt IS NULL;

                    WAITFOR DELAY '00:00:01'; -- Small delay to reduce lock pressure
                END
            ");
        });

        // Since IfDatabase doesn't have a "not" operator, we'll handle common alternatives explicitly
        IfDatabase(ProcessorIdConstants.Postgres, ProcessorIdConstants.MySql, ProcessorIdConstants.SQLite).Execute.Sql(@"
            UPDATE Users
            SET UpdatedAt = CURRENT_TIMESTAMP
            WHERE UpdatedAt IS NULL
        ");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET UpdatedAt = NULL");
    }
}
```

### Error Handling in SQL

```csharp
[Migration(4)]
public class ErrorHandlingMigration : Migration
{
    public override void Up()
    {
        IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
            BEGIN TRY
                -- Risky operation
                ALTER TABLE Users ADD CONSTRAINT CK_Users_Email CHECK (Email LIKE '%@%')
            END TRY
            BEGIN CATCH
                -- Log error or handle gracefully
                PRINT 'Failed to add email constraint: ' + ERROR_MESSAGE()
            END CATCH
        ");
    }

    public override void Down()
    {
        Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Email");
    }
}
```

### Dynamic SQL Generation

```csharp
[Migration(5)]
public class DynamicSqlMigration : Migration
{
    public override void Up()
    {
        // Generate SQL based on schema inspection
        Execute.Sql(@"
            DECLARE @sql NVARCHAR(MAX) = ''

            SELECT @sql = @sql + 'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']
                                 ADD CreatedBy NVARCHAR(100) NULL;' + CHAR(13)
            FROM INFORMATION_SCHEMA.TABLES t
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND NOT EXISTS (
                  SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c
                  WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA
                    AND c.TABLE_NAME = t.TABLE_NAME
                    AND c.COLUMN_NAME = 'CreatedBy'
              )

            EXEC sp_executesql @sql
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DECLARE @sql NVARCHAR(MAX) = ''

            SELECT @sql = @sql + 'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']
                                 DROP COLUMN CreatedBy;' + CHAR(13)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE COLUMN_NAME = 'CreatedBy'

            EXEC sp_executesql @sql
        ");
    }
}
```

## Database-Specific Operations

### SQL Server Specific Features

```csharp
public override void Up()
{
    IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
        // Enable features
        Execute.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON");
        Execute.Sql("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON");

        // Create full-text catalog
        Execute.Sql("CREATE FULLTEXT CATALOG DocumentCatalog AS DEFAULT");
    });
}
```

### PostgreSQL Specific Features

```csharp
public override void Up()
{
    IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
        // Create extension
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");

        // Create custom type
        Execute.Sql("CREATE TYPE user_status AS ENUM ('active', 'inactive', 'suspended')");
    });
}
```

## Best Practices and Limitations

### What Execute.Sql Should Be Used For

**✅ Good uses:**
- Database-specific operations not supported by the fluent API
- Complex data transformations during migration
- Bulk operations on existing data
- Creating database-specific objects (stored procedures, functions, views)
- Performance-critical operations requiring custom SQL

**❌ Avoid for:**
- Simple schema changes (use fluent API instead)
- Cross-database compatible operations
- Operations that might be rolled back frequently

### Performance Considerations

```csharp
public override void Up()
{
    // ❌ Bad: Can cause locks and timeouts
    Execute.Sql("UPDATE Users SET IsActive = 1"); // Updates entire table at once

    // ✅ Good: Batch processing
    Execute.Sql(@"
        WHILE @@ROWCOUNT > 0
        BEGIN
            UPDATE TOP (1000) Users
            SET IsActive = 1
            WHERE IsActive <> 1 OR IsActive IS NULL

            WAITFOR DELAY '00:00:01'  -- Give other operations a chance
        END
    ");
}
```

### SQL Injection Prevention

```csharp
public override void Up()
{
    // ❌ Bad: Potential SQL injection if value comes from external source
    var tableName = GetTableNameFromSomewhere();
    Execute.Sql($"DROP TABLE {tableName}");

    // ✅ Good: Validate input or use safer approaches
    var allowedTables = new[] { "TempTable1", "TempTable2" };
    if (allowedTables.Contains(tableName))
    {
        Execute.Sql($"DROP TABLE {tableName}");
    }
}
```

### Cross-Database Compatibility

```csharp
public override void Up()
{
    // Handle database differences explicitly
    IfDatabase(ProcessorIdConstants.SqlServer)
        .Execute.Sql("SELECT TOP 10 * FROM Users");

    IfDatabase(ProcessorIdConstants.Postgres, ProcessorIdConstants.MySql)
        .Execute.Sql("SELECT * FROM Users LIMIT 10");

    IfDatabase(ProcessorIdConstants.SQLite)
        .Execute.Sql("SELECT * FROM Users LIMIT 10");
}
```

## Integration with Migration Features

### Working with Tags

```csharp
[Migration(1)]
[Tags("DataMigration", "Production")]
public class TaggedSqlMigration : Migration
{
    public override void Up()
    {
        // This SQL will only run when appropriate tags are specified
        Execute.Sql(@"
            UPDATE UserSettings
            SET Theme = 'Dark'
            WHERE Theme IS NULL
        ");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE UserSettings SET Theme = NULL WHERE Theme = 'Dark'");
    }
}
```

### Working with Profiles

```csharp
[Migration(1)]
[Profile("DataSeed")]
public class DataSeedingSql : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            INSERT INTO Users (Username, Email, IsActive, CreatedAt)
            VALUES
                ('admin', 'admin@company.com', 1, GETDATE()),
                ('testuser', 'test@company.com', 1, GETDATE())
        ");
    }

    public override void Down()
    {
        Execute.Sql("DELETE FROM Users WHERE Username IN ('admin', 'testuser')");
    }
}
```

### Maintenance Migrations with SQL

```csharp
[Migration(1)]
[MaintenanceMigration(TransactionBehavior.None)] // Some operations can't be in transactions
public class MaintenanceSql : Migration
{
    public override void Up()
    {
        IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
            -- Rebuild indexes
            DECLARE @sql NVARCHAR(MAX) = ''
            SELECT @sql = @sql + 'ALTER INDEX ALL ON [' + s.name + '].[' + t.name + '] REBUILD;' + CHAR(13)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id

            EXEC sp_executesql @sql
        ");
    }

    public override void Down()
    {
        // Maintenance operations typically don't have meaningful rollbacks
        Execute.Sql("-- Maintenance migration - no rollback needed");
    }
}
```

## Error Handling and Recovery

### Handling Migration Failures

```csharp
[Migration(1)]
public class SafeSqlMigration : Migration
{
    public override void Up()
    {
        // Log migration start
        Execute.Sql("INSERT INTO MigrationLog (Migration, Status, StartTime) VALUES ('SafeSqlMigration', 'Started', GETDATE())");

        try
        {
            // Your migration logic here
            Execute.Sql("UPDATE Users SET IsActive = 1 WHERE IsActive IS NULL");

            // Log success
            Execute.Sql("UPDATE MigrationLog SET Status = 'Completed', EndTime = GETDATE() WHERE Migration = 'SafeSqlMigration'");
        }
        catch
        {
            // Log failure (in a real implementation, you'd need custom processor support for this)
            Execute.Sql("UPDATE MigrationLog SET Status = 'Failed', EndTime = GETDATE() WHERE Migration = 'SafeSqlMigration'");
            throw;
        }
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET IsActive = NULL WHERE IsActive = 1");
        Execute.Sql("INSERT INTO MigrationLog (Migration, Status, StartTime, EndTime) VALUES ('SafeSqlMigration', 'Rolled Back', GETDATE(), GETDATE())");
    }
}
```

### Recovery Strategies

When Execute.Sql migrations fail, you may need manual intervention:

```sql
-- recovery_checklist.sql
-- 1. Check current database state
SELECT * FROM VersionInfo WHERE Version = 20240101120000;

-- 2. Verify partial changes
SELECT COUNT(*) FROM Users WHERE IsActive = 1;

-- 3. Clean up if needed
UPDATE Users SET IsActive = NULL WHERE IsActive = 1;

-- 4. Reset migration state (if needed)
DELETE FROM VersionInfo WHERE Version = 20240101120000;
```
