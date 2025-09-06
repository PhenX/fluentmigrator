# Raw SQL (Scripts & Helpers)

FluentMigrator provides powerful tools for executing raw SQL when the fluent API doesn't meet your specific needs. This guide covers all Execute.Sql methods, RawSql helpers, and best practices for custom SQL operations.

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
    IfDatabase("SqlServer").Execute.Sql(@"
        UPDATE Users 
        SET LastLogin = GETUTCDATE()
        WHERE Email LIKE '%@company.com'");

    IfDatabase("Postgres").Execute.Sql(@"
        UPDATE Users 
        SET LastLogin = NOW()
        WHERE Email LIKE '%@company.com'");

    IfDatabase("MySQL").Execute.Sql(@"
        UPDATE Users 
        SET LastLogin = UTC_TIMESTAMP()
        WHERE Email LIKE '%@company.com'");
}
```

### SQL Scripts from Files

```csharp
// Execute script from embedded resource
Execute.EmbeddedScript("Scripts.CreateStoredProcedures.sql");

// Execute script from file system
Execute.Script(@"C:\Scripts\DataMigration.sql");
```

## Data Querying with Returns

### Simple Value Returns

```csharp
// Get a single integer value
var userCount = Execute.Sql("SELECT COUNT(*) FROM Users")
    .Returns<int>()
    .FirstOrDefault();

// Get multiple values
var userIds = Execute.Sql("SELECT Id FROM Users WHERE IsActive = 1")
    .Returns<int>()
    .ToList();

// Get string values
var userNames = Execute.Sql("SELECT Username FROM Users ORDER BY Username")
    .Returns<string>()
    .ToList();
```

### Complex Object Returns

```csharp
// Define a class for complex returns
public class UserSummary
{
    public int Id { get; set; }
    public string Username { get; set; }
    public DateTime? LastLogin { get; set; }
    public int OrderCount { get; set; }
}

// Query and map to objects
var userSummaries = Execute.Sql(@"
    SELECT 
        u.Id,
        u.Username,
        u.LastLogin,
        COUNT(o.Id) as OrderCount
    FROM Users u
    LEFT JOIN Orders o ON u.Id = o.UserId
    GROUP BY u.Id, u.Username, u.LastLogin")
    .Returns<UserSummary>()
    .ToList();
```

### Validation and Error Checking

```csharp
public override void Up()
{
    // Validate data before making changes
    var invalidEmailCount = Execute.Sql(@"
        SELECT COUNT(*) 
        FROM Users 
        WHERE Email IS NULL OR Email = '' OR Email NOT LIKE '%@%'")
        .Returns<int>()
        .FirstOrDefault();
        
    if (invalidEmailCount > 0)
    {
        throw new InvalidOperationException(
            $"Found {invalidEmailCount} users with invalid email addresses. " +
            "Please fix data before running this migration.");
    }
    
    // Safe to proceed
    Execute.Sql("UPDATE Users SET Email = LOWER(Email)");
}
```

## RawSql Helper for Insert Operations

### Basic RawSql.Insert Usage

When you need to use database functions in insert operations, `RawSql.Insert()` allows you to embed SQL expressions:

```csharp
// Insert with database functions
Insert.IntoTable("AuditLog").Row(new
{
    Action = "UserLogin",
    Timestamp = RawSql.Insert("GETUTCDATE()"),     // SQL Server
    Username = RawSql.Insert("SUSER_SNAME()"),     // Current database user
    SessionId = RawSql.Insert("@@SPID")            // Session ID
});
```

### Database-Specific Functions

#### SQL Server Functions
```csharp
Insert.IntoTable("Events").Row(new
{
    EventId = RawSql.Insert("NEWID()"),            // UUID generation
    EventTime = RawSql.Insert("GETUTCDATE()"),     // UTC timestamp
    MachineName = RawSql.Insert("HOST_NAME()"),    // Machine name
    DatabaseName = RawSql.Insert("DB_NAME()"),     // Current database
    Username = RawSql.Insert("SUSER_SNAME()")      // Current user
});
```

#### PostgreSQL Functions
```csharp
Insert.IntoTable("Sessions").Row(new
{
    SessionId = RawSql.Insert("gen_random_uuid()"), // UUID generation
    CreatedAt = RawSql.Insert("NOW()"),             // Current timestamp
    Username = RawSql.Insert("current_user"),       // Current user
    DatabaseName = RawSql.Insert("current_database()"), // Database name
    ProcessId = RawSql.Insert("pg_backend_pid()")   // Process ID
});
```

#### MySQL Functions
```csharp
Insert.IntoTable("LoginHistory").Row(new
{
    LoginId = RawSql.Insert("UUID()"),             // UUID generation
    LoginTime = RawSql.Insert("UTC_TIMESTAMP()"),  // UTC timestamp
    Username = RawSql.Insert("USER()"),            // Current user
    ConnectionId = RawSql.Insert("CONNECTION_ID()"), // Connection ID
    Version = RawSql.Insert("VERSION()")           // MySQL version
});
```

#### SQLite Functions
```csharp
Insert.IntoTable("Events").Row(new
{
    EventTime = RawSql.Insert("datetime('now')"),   // Current datetime
    EventDate = RawSql.Insert("date('now')"),       // Current date
    Timestamp = RawSql.Insert("strftime('%s','now')"), // Unix timestamp
    RandomId = RawSql.Insert("hex(randomblob(16))") // Random hex string
});
```

### Cross-Database RawSql Usage

```csharp
public override void Up()
{
    Create.Table("SystemEvents")
        .WithIdColumn()
        .WithColumn("EventTime").AsDateTime().NotNullable()
        .WithColumn("Username").AsString(100).NotNullable()
        .WithColumn("EventType").AsString(50).NotNullable();

    // Insert initial record with database-specific functions
    IfDatabase("SqlServer").Insert.IntoTable("SystemEvents").Row(new
    {
        EventTime = RawSql.Insert("GETUTCDATE()"),
        Username = RawSql.Insert("SUSER_SNAME()"),
        EventType = "SystemInitialized"
    });

    IfDatabase("Postgres").Insert.IntoTable("SystemEvents").Row(new
    {
        EventTime = RawSql.Insert("NOW()"),
        Username = RawSql.Insert("current_user"),
        EventType = "SystemInitialized"
    });

    IfDatabase("MySQL").Insert.IntoTable("SystemEvents").Row(new
    {
        EventTime = RawSql.Insert("UTC_TIMESTAMP()"),
        Username = RawSql.Insert("USER()"),
        EventType = "SystemInitialized"
    });
}
```

## Advanced Execute.Sql Patterns

### Batch Processing for Large Datasets

```csharp
public override void Up()
{
    // Check dataset size
    var recordCount = Execute.Sql("SELECT COUNT(*) FROM Users")
        .Returns<int>()
        .FirstOrDefault();
    
    if (recordCount > 100000)
    {
        // Use batch processing for large datasets
        IfDatabase("SqlServer").Delegate(() =>
        {
            Execute.Sql(@"
                DECLARE @BatchSize INT = 5000;
                WHILE EXISTS (SELECT 1 FROM Users WHERE UpdatedAt IS NULL)
                BEGIN
                    UPDATE TOP (@BatchSize) Users 
                    SET UpdatedAt = GETDATE()
                    WHERE UpdatedAt IS NULL;
                    
                    -- Prevent blocking
                    WAITFOR DELAY '00:00:01';
                END");
        });
        
        // For other databases, use smaller batches - but we need a way to handle "not SqlServer"
        // Since IfDatabase doesn't have a "not" operator, we'll handle common alternatives explicitly
        IfDatabase("Postgres", "MySQL", "SQLite").Execute.Sql(@"
            UPDATE Users 
            SET UpdatedAt = NOW()
            WHERE UpdatedAt IS NULL
            LIMIT 5000");
    }
    else
    {
        // Process all at once for smaller datasets
        Execute.Sql("UPDATE Users SET UpdatedAt = NOW() WHERE UpdatedAt IS NULL");
    }
}
```

### Transaction Control in SQL

```csharp
public override void Up()
{
    IfDatabase("SqlServer").Execute.Sql(@"
        BEGIN TRANSACTION;
        
        BEGIN TRY
            -- Complex multi-step operation
            UPDATE Users SET Status = 'Migrated' WHERE Status = 'Active';
            
            INSERT INTO UserHistory (UserId, Action, Timestamp)
            SELECT Id, 'StatusChanged', GETDATE() FROM Users WHERE Status = 'Migrated';
            
            -- Verify results
            IF @@ROWCOUNT = 0
                THROW 50001, 'No rows were migrated', 1;
                
            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION;
            THROW;
        END CATCH");

    // FluentMigrator handles transactions automatically for other databases
    IfDatabase("Postgres", "MySQL", "SQLite").Delegate(() =>
    {
        Execute.Sql("UPDATE Users SET Status = 'Migrated' WHERE Status = 'Active'");
        
        Execute.Sql(@"
            INSERT INTO UserHistory (UserId, Action, Timestamp)
            SELECT Id, 'StatusChanged', NOW() FROM Users WHERE Status = 'Migrated'");
    });
}
```

### Data Migration and Transformation

```csharp
public override void Up()
{
    // Add new column
    Alter.Table("Users").AddColumn("FullName").AsString(200).Nullable();
    
    // Migrate existing data using SQL
    Execute.Sql(@"
        UPDATE Users 
        SET FullName = TRIM(COALESCE(FirstName, '') + ' ' + COALESCE(LastName, ''))
        WHERE FullName IS NULL");
        
    // Handle edge cases
    Execute.Sql(@"
        UPDATE Users 
        SET FullName = CASE 
            WHEN FullName = '' THEN 'Unknown'
            WHEN LEN(FullName) > 200 THEN LEFT(FullName, 197) + '...'
            ELSE FullName
        END");
        
    // Make column not nullable after migration
    Alter.Column("FullName").OnTable("Users").AsString(200).NotNullable();
}
```

### Database-Specific Operations

#### SQL Server Specific
```csharp
IfDatabase("SqlServer").Delegate(() =>
{
    // Create full-text catalog and index
    Execute.Sql("CREATE FULLTEXT CATALOG DocumentsCatalog AS DEFAULT");
    Execute.Sql(@"
        CREATE FULLTEXT INDEX ON Documents(Title, Content)
        KEY INDEX PK_Documents ON DocumentsCatalog");
        
    // Create computed columns
    Execute.Sql(@"
        ALTER TABLE Orders 
        ADD TotalWithTax AS (Subtotal * (1 + TaxRate)) PERSISTED");
        
    // Create indexed views
    Execute.Sql(@"
        CREATE VIEW OrderSummaryView WITH SCHEMABINDING AS
        SELECT 
            CustomerId, 
            COUNT_BIG(*) as OrderCount,
            SUM(Total) as TotalAmount
        FROM dbo.Orders
        GROUP BY CustomerId");
        
    Execute.Sql("CREATE UNIQUE CLUSTERED INDEX IX_OrderSummary ON OrderSummaryView(CustomerId)");
});
```

#### PostgreSQL Specific
```csharp
IfDatabase("Postgres").Delegate(() =>
{
    // Create custom data types
    Execute.Sql("CREATE TYPE order_status AS ENUM ('pending', 'processing', 'shipped', 'delivered')");
    
    // Create extensions
    Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");
    Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\"");
    
    // Create GIN indexes for full-text search
    Execute.Sql(@"
        ALTER TABLE Documents 
        ADD COLUMN search_vector tsvector");
        
    Execute.Sql(@"
        UPDATE Documents 
        SET search_vector = to_tsvector('english', Title || ' ' || COALESCE(Content, ''))");
        
    Execute.Sql("CREATE INDEX IX_Documents_Search ON Documents USING GIN (search_vector)");
});
```

## Best Practices and Limitations

### ✅ Best Practices

1. **Use Execute.Sql for complex operations** not supported by fluent API
2. **Combine with IfDatabase()** for cross-database compatibility using fluent syntax like `IfDatabase("SqlServer").Execute.Sql(...)`
3. **Validate data before operations** using Returns() methods
4. **Handle large datasets efficiently** with batch processing
5. **Document database-specific dependencies** in migration comments
6. **Test thoroughly** with your target database(s)

### ❌ Common Mistakes

1. **Don't use RawSql.Insert in Update/Delete operations** - it only works with Insert
2. **Don't hardcode database-specific syntax** without conditional logic
3. **Don't ignore transaction boundaries** - large operations might timeout
4. **Don't skip error handling** for critical data operations

### RawSql.Insert Limitations

::: warning Important Limitations
The `RawSql.Insert()` method has specific limitations:

- **Insert operations only**: Cannot be used in Update or Delete operations
- **No type checking**: SQL expressions are passed through without validation
- **Database-specific**: Makes your migrations dependent on specific database features
:::

```csharp
// ❌ These will NOT work
Update.Table("Users")
    .Set(new { LastLogin = RawSql.Insert("GETDATE()") }) // Won't work!
    .Where(new { Id = 1 });

Delete.FromTable("Users")
    .Row(new { CreatedAt = RawSql.Insert("< DATEADD(day, -30, GETDATE())") }); // Won't work!

// ✅ Use Execute.Sql instead
Execute.Sql("UPDATE Users SET LastLogin = GETDATE() WHERE Id = 1");
Execute.Sql("DELETE FROM Users WHERE CreatedAt < DATEADD(day, -30, GETDATE())");
```

### Database-Agnostic Alternatives

When possible, prefer FluentMigrator's built-in methods:

```csharp
// ✅ Database-agnostic (preferred)
.WithColumn("CreatedAt")
    .AsDateTime()
    .NotNullable()
    .WithDefaultValue(SystemMethods.CurrentDateTime)

// vs database-specific RawSql
// ❌ Database-specific (use only when necessary)
Execute.Sql("ALTER TABLE Users ADD CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE() FOR CreatedAt");
```

## Integration with Migration Features

### With Profiles and Tags

```csharp
[Profile("Development")]
[Tags("DataSeeding")]
public class SeedDevelopmentData : Migration
{
    public override void Up()
    {
        // Seed test data using database functions
        Execute.Sql(@"
            INSERT INTO Users (Username, Email, CreatedAt, PasswordHash)
            VALUES 
                ('testuser1', 'test1@example.com', GETDATE(), HASHBYTES('SHA2_256', 'password123')),
                ('testuser2', 'test2@example.com', GETDATE(), HASHBYTES('SHA2_256', 'password456'))");
                
        // Insert using RawSql for specific values
        Insert.IntoTable("UserProfiles").Row(new
        {
            UserId = 1,
            ProfileData = "{'theme': 'dark'}",
            CreatedAt = RawSql.Insert("GETDATE()"),
            LastModified = RawSql.Insert("GETDATE()")
        });
    }
    
    public override void Down()
    {
        Execute.Sql("DELETE FROM UserProfiles WHERE UserId IN (1, 2)");
        Execute.Sql("DELETE FROM Users WHERE Username IN ('testuser1', 'testuser2')");
    }
}
```

### With Maintenance Migrations

```csharp
[Maintenance(MigrationStage.AfterAll)]
public class UpdateStatistics : Migration
{
    public override void Up()
    {
        IfDatabase("SqlServer").Delegate(() =>
        {
            // Update table statistics
            Execute.Sql("UPDATE STATISTICS Users");
            Execute.Sql("UPDATE STATISTICS Orders");
            Execute.Sql("UPDATE STATISTICS Products");
            
            // Rebuild fragmented indexes
            Execute.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql = @sql + 'ALTER INDEX ' + i.name + ' ON ' + t.name + ' REBUILD;'
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE i.avg_fragmentation_in_percent > 30;
                
                EXEC sp_executesql @sql;");
        });

        IfDatabase("Postgres").Delegate(() =>
        {
            Execute.Sql("VACUUM ANALYZE Users");
            Execute.Sql("VACUUM ANALYZE Orders");
            Execute.Sql("VACUUM ANALYZE Products");
        });
    }
    
    public override void Down() { /* Maintenance operations typically don't need rollback */ }
}
```

## Error Handling and Recovery

### Defensive Programming

```csharp
public override void Up()
{
    try
    {
        // Backup critical data before changes
        Execute.Sql(@"
            SELECT * INTO Users_Backup_" + DateTime.Now.ToString("yyyyMMdd") + @"
            FROM Users 
            WHERE IsActive = 0");
            
        // Perform risky operation
        Execute.Sql("UPDATE Users SET Status = 'Archived' WHERE IsActive = 0");
        
        // Verify results
        var updatedCount = Execute.Sql("SELECT @@ROWCOUNT").Returns<int>().FirstOrDefault();
        if (updatedCount == 0)
        {
            throw new InvalidOperationException("No users were updated - operation may have failed");
        }
        
        // Log the operation
        Execute.Sql(@"
            INSERT INTO MigrationLog (Migration, Action, RecordCount, Timestamp)
            VALUES ('UpdateUserStatus', 'Archive', " + updatedCount + ", GETDATE())");
    }
    catch (Exception ex)
    {
        // Transaction will be rolled back automatically
        throw new InvalidOperationException($"User status update failed: {ex.Message}", ex);
    }
}
```

Raw SQL operations in FluentMigrator provide powerful flexibility when the fluent API limitations require direct database access. Use them judiciously while maintaining code readability and database portability where possible.

## See Also

- [Columns](managing-columns.md) - Column management using fluent API
- [Data Operations](operations/data.md) - Working with data using fluent methods  
- [Best Practices](advanced/best-practices.md) - Migration best practices
- [Database-Specific Features](advanced/dbms-extensions.md) - Platform-specific capabilities