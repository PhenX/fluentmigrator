# SQLite

SQLite is a lightweight, file-based database engine that's perfect for development, testing, and applications requiring an embedded database. FluentMigrator provides comprehensive SQLite support with considerations for its unique characteristics and limitations.

## Getting Started with SQLite

### Installation

Install the SQLite provider package:

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.SQLite

# For Package Manager Console
Install-Package FluentMigrator.Runner.SQLite
```

### Basic Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

### Connection String Examples

```csharp
// File-based database
"Data Source=./database.sqlite;"

// In-memory database (for testing)
"Data Source=:memory:;"

// With additional options
"Data Source=./myapp.db;Version=3;New=False;Compress=True;"

// Read-only database
"Data Source=./readonly.db;Mode=ReadOnly;"

// Shared cache mode
"Data Source=./shared.db;Cache=Shared;"
```

## SQLite Data Types and Limitations

### SQLite Type System

SQLite uses dynamic typing with type affinity. FluentMigrator maps .NET types to SQLite appropriately:

```csharp
public class SQLiteDataTypes : Migration
{
    public override void Up()
    {
        Create.Table("SQLiteTypeExamples")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            
            // INTEGER types (stored as signed integers)
            .WithColumn("IntegerValue").AsInt32().NotNullable()
            .WithColumn("LongValue").AsInt64().NotNullable()
            .WithColumn("BooleanValue").AsBoolean().NotNullable() // Stored as INTEGER (0/1)
            
            // REAL types (stored as floating point)
            .WithColumn("FloatValue").AsFloat().NotNullable()
            .WithColumn("DoubleValue").AsDouble().NotNullable()
            .WithColumn("DecimalValue").AsDecimal(10, 2).NotNullable() // Stored as TEXT or REAL
            
            // TEXT types
            .WithColumn("StringValue").AsString(255).NotNullable()
            .WithColumn("FixedLengthString").AsFixedLengthString(10).NotNullable()
            .WithColumn("TextValue").AsCustom("TEXT").Nullable()
            
            // BLOB types
            .WithColumn("BinaryValue").AsBinary().Nullable()
            .WithColumn("BlobValue").AsCustom("BLOB").Nullable()
            
            // Date and time (stored as TEXT, REAL, or INTEGER)
            .WithColumn("DateTimeValue").AsDateTime().NotNullable()
            .WithColumn("DateValue").AsDate().Nullable()
            .WithColumn("TimeValue").AsTime().Nullable()
            
            // GUID (stored as TEXT)
            .WithColumn("GuidValue").AsGuid().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("SQLiteTypeExamples");
    }
}
```

### Understanding SQLite Limitations

```csharp
public class SQLiteLimitations : Migration
{
    public override void Up()
    {
        // SQLite limitations to be aware of:
        
        // 1. No ALTER COLUMN support - must recreate table
        Create.Table("LimitationExample")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Value").AsInt32().NotNullable();
            
        // 2. No DROP COLUMN support (pre SQLite 3.35.0)
        // This will work in FluentMigrator by recreating the table behind the scenes
        
        // 3. Limited foreign key constraint enforcement
        // Foreign keys must be enabled with PRAGMA foreign_keys = ON
        
        // 4. No RIGHT JOIN or FULL OUTER JOIN
        
        // 5. No stored procedures or user-defined functions (in standard SQLite)
        
        // 6. Limited ALTER TABLE support
        // Only ADD COLUMN and RENAME TABLE are supported natively
        
        Insert.IntoTable("LimitationExample")
            .Row(new { Name = "Example", Value = 100 });
    }

    public override void Down()
    {
        Delete.Table("LimitationExample");
    }
}
```

## Working Around SQLite Limitations

### Column Modifications (Recreate Table Pattern)

```csharp
public class SQLiteColumnModification : Migration
{
    public override void Up()
    {
        // Create initial table
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Email").AsString(100).NotNullable()
            .WithColumn("Age").AsInt32().NotNullable();
            
        Insert.IntoTable("Users")
            .Row(new { Name = "John Doe", Email = "john@example.com", Age = 30 })
            .Row(new { Name = "Jane Smith", Email = "jane@example.com", Age = 25 });
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}

public class SQLiteAlterColumn : Migration
{
    public override void Up()
    {
        // FluentMigrator handles this automatically by recreating the table
        // but you can also do it manually for better control
        
        // Method 1: Let FluentMigrator handle it (recommended)
        Alter.Column("Email").OnTable("Users")
            .AsString(255).NotNullable(); // Change from 100 to 255
            
        // Method 2: Manual table recreation (for complex scenarios)
        /*
        // Step 1: Create new table with desired structure
        Create.Table("Users_New")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable() // New size
            .WithColumn("Age").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active"); // New column
            
        // Step 2: Copy data
        Execute.Sql(@"
            INSERT INTO Users_New (Id, Name, Email, Age, Status)
            SELECT Id, Name, Email, Age, 'Active' FROM Users");
            
        // Step 3: Drop old table and rename new table
        Delete.Table("Users");
        Rename.Table("Users_New").To("Users");
        */
    }

    public override void Down()
    {
        Alter.Column("Email").OnTable("Users")
            .AsString(100).NotNullable();
    }
}
```

### Adding and Dropping Columns

```csharp
public class SQLiteAddDropColumns : Migration
{
    public override void Up()
    {
        // Adding columns is supported natively
        Alter.Table("Users")
            .AddColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("LastLoginAt").AsDateTime().Nullable();
            
        // Update existing records
        Execute.Sql("UPDATE Users SET CreatedAt = datetime('now') WHERE CreatedAt IS NULL");
        
        // Dropping columns - FluentMigrator will recreate the table
        // Delete.Column("Age").FromTable("Users"); // This works but recreates table
    }

    public override void Down()
    {
        Delete.Column("LastLoginAt").FromTable("Users");
        Delete.Column("IsActive").FromTable("Users");
        Delete.Column("CreatedAt").FromTable("Users");
    }
}
```

## SQLite-Specific Features

### Working with SQLite Pragmas

```csharp
public class SQLitePragmas : Migration
{
    public override void Up()
    {
        // Enable foreign key constraints
        Execute.Sql("PRAGMA foreign_keys = ON");
        
        // Set journal mode for better concurrency
        Execute.Sql("PRAGMA journal_mode = WAL");
        
        // Set synchronous mode for better performance
        Execute.Sql("PRAGMA synchronous = NORMAL");
        
        // Set cache size (in pages, negative for KB)
        Execute.Sql("PRAGMA cache_size = -64000"); // 64MB cache
        
        // Enable recursive triggers
        Execute.Sql("PRAGMA recursive_triggers = ON");
        
        // Set temp store to memory for better performance
        Execute.Sql("PRAGMA temp_store = MEMORY");
        
        // Create tables after setting pragmas
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable();
            
        Create.Table("OrderItems")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductName").AsString(100).NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable();
            
        // Foreign key will be enforced because we enabled foreign_keys pragma
        Create.ForeignKey("FK_OrderItems_Orders")
            .FromTable("OrderItems").ForeignColumn("OrderId")
            .ToTable("Orders").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_OrderItems_Orders").OnTable("OrderItems");
        Delete.Table("OrderItems");
        Delete.Table("Orders");
    }
}
```

### SQLite Full-Text Search (FTS)

```csharp
public class SQLiteFullTextSearch : Migration
{
    public override void Up()
    {
        // Create regular table for documents
        Create.Table("Documents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom("TEXT").NotNullable()
            .WithColumn("Author").AsString(100).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // Create FTS5 virtual table for full-text search
        Execute.Sql(@"
            CREATE VIRTUAL TABLE documents_fts USING fts5(
                title, 
                content, 
                author, 
                content='Documents', 
                content_rowid='Id'
            )");
            
        // Create triggers to keep FTS table in sync
        Execute.Sql(@"
            CREATE TRIGGER documents_ai AFTER INSERT ON Documents BEGIN
              INSERT INTO documents_fts(rowid, title, content, author) 
              VALUES (new.Id, new.Title, new.Content, new.Author);
            END");
            
        Execute.Sql(@"
            CREATE TRIGGER documents_ad AFTER DELETE ON Documents BEGIN
              INSERT INTO documents_fts(documents_fts, rowid, title, content, author) 
              VALUES('delete', old.Id, old.Title, old.Content, old.Author);
            END");
            
        Execute.Sql(@"
            CREATE TRIGGER documents_au AFTER UPDATE ON Documents BEGIN
              INSERT INTO documents_fts(documents_fts, rowid, title, content, author) 
              VALUES('delete', old.Id, old.Title, old.Content, old.Author);
              INSERT INTO documents_fts(rowid, title, content, author) 
              VALUES (new.Id, new.Title, new.Content, new.Author);
            END");
            
        // Insert sample data
        Insert.IntoTable("Documents")
            .Row(new
            {
                Title = "SQLite Full-Text Search Guide",
                Content = "This document explains how to implement full-text search using SQLite FTS5 extension with comprehensive examples.",
                Author = "Database Expert",
                CreatedAt = DateTime.Now
            })
            .Row(new
            {
                Title = "Database Migration Best Practices",
                Content = "Learn the best practices for database migrations including versioning, testing, and deployment strategies.",
                Author = "Migration Specialist",
                CreatedAt = DateTime.Now.AddDays(-1)
            });
            
        // Example search queries (for documentation)
        Execute.Sql(@"
            -- Full-text search examples:
            
            -- Basic search
            -- SELECT * FROM documents_fts WHERE documents_fts MATCH 'sqlite';
            
            -- Search with ranking
            -- SELECT d.*, rank FROM Documents d
            -- JOIN (SELECT rowid, rank FROM documents_fts WHERE documents_fts MATCH 'database' ORDER BY rank) fts
            -- ON d.Id = fts.rowid;
            
            -- Boolean search
            -- SELECT * FROM documents_fts WHERE documents_fts MATCH 'sqlite AND migration';
            
            -- Phrase search
            -- SELECT * FROM documents_fts WHERE documents_fts MATCH '""best practices""';
            
            -- Field-specific search
            -- SELECT * FROM documents_fts WHERE documents_fts MATCH 'title:guide';
            ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER IF EXISTS documents_au");
        Execute.Sql("DROP TRIGGER IF EXISTS documents_ad");
        Execute.Sql("DROP TRIGGER IF EXISTS documents_ai");
        Execute.Sql("DROP TABLE IF EXISTS documents_fts");
        Delete.Table("Documents");
    }
}
```

### SQLite JSON Support (3.38.0+)

```csharp
public class SQLiteJsonSupport : Migration
{
    public override void Up()
    {
        // Check if JSON functions are available
        var hasJsonSupport = true;
        try
        {
            Execute.Sql("SELECT json('{}')");
        }
        catch
        {
            hasJsonSupport = false;
        }
        
        if (hasJsonSupport)
        {
            Create.Table("UserProfiles")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ProfileData").AsCustom("TEXT").Nullable() // JSON stored as TEXT
                .WithColumn("Preferences").AsCustom("TEXT").Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable();
                
            // Insert JSON data
            Insert.IntoTable("UserProfiles")
                .Row(new
                {
                    UserId = 1,
                    ProfileData = @"{
                        ""name"": ""John Doe"",
                        ""age"": 30,
                        ""email"": ""john@example.com"",
                        ""address"": {
                            ""street"": ""123 Main St"",
                            ""city"": ""Anytown"",
                            ""state"": ""CA"",
                            ""zip"": ""12345""
                        },
                        ""hobbies"": [""reading"", ""gaming"", ""hiking""]
                    }",
                    Preferences = @"{
                        ""theme"": ""dark"",
                        ""language"": ""en"",
                        ""notifications"": {
                            ""email"": true,
                            ""sms"": false,
                            ""push"": true
                        }
                    }",
                    CreatedAt = DateTime.Now
                });
                
            // Create indexes on JSON fields (using generated columns for SQLite 3.31+)
            Execute.Sql(@"
                ALTER TABLE UserProfiles 
                ADD COLUMN name_extracted TEXT GENERATED ALWAYS AS (json_extract(ProfileData, '$.name')) VIRTUAL");
                
            Execute.Sql(@"
                CREATE INDEX IX_UserProfiles_Name ON UserProfiles (name_extracted)");
                
            // Example JSON queries (for documentation)
            Execute.Sql(@"
                -- JSON query examples for SQLite:
                
                -- Extract JSON values
                -- SELECT 
                --     UserId,
                --     json_extract(ProfileData, '$.name') as name,
                --     json_extract(ProfileData, '$.email') as email,
                --     json_extract(ProfileData, '$.age') as age
                -- FROM UserProfiles;
                
                -- Query JSON arrays
                -- SELECT * FROM UserProfiles 
                -- WHERE EXISTS (
                --     SELECT 1 FROM json_each(json_extract(ProfileData, '$.hobbies')) 
                --     WHERE value = 'gaming'
                -- );
                
                -- Update JSON data
                -- UPDATE UserProfiles 
                -- SET ProfileData = json_set(ProfileData, '$.age', 31)
                -- WHERE UserId = 1;
                
                -- Add JSON property
                -- UPDATE UserProfiles 
                -- SET ProfileData = json_insert(ProfileData, '$.lastLogin', datetime('now'))
                -- WHERE UserId = 1;
                ");
        }
        else
        {
            // Fallback for older SQLite versions without JSON support
            Create.Table("UserProfiles")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Name").AsString(100).Nullable()
                .WithColumn("Email").AsString(255).Nullable()
                .WithColumn("Age").AsInt32().Nullable()
                .WithColumn("ConfigData").AsCustom("TEXT").Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable();
        }
    }

    public override void Down()
    {
        Delete.Table("UserProfiles");
    }
}
```

## SQLite Performance Optimization

### Indexes and Query Optimization

```csharp
public class SQLitePerformanceOptimization : Migration
{
    public override void Up()
    {
        // Set optimal pragmas for performance
        Execute.Sql("PRAGMA journal_mode = WAL");
        Execute.Sql("PRAGMA synchronous = NORMAL");
        Execute.Sql("PRAGMA cache_size = -64000"); // 64MB
        Execute.Sql("PRAGMA temp_store = MEMORY");
        Execute.Sql("PRAGMA mmap_size = 268435456"); // 256MB memory-mapped I/O
        
        Create.Table("HighVolumeData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
            .WithColumn("SearchText").AsCustom("TEXT").Nullable();
            
        // Create optimal indexes
        Create.Index("IX_HighVolumeData_CategoryId")
            .OnTable("HighVolumeData")
            .OnColumn("CategoryId");
            
        Create.Index("IX_HighVolumeData_Status_CreatedAt")
            .OnTable("HighVolumeData")
            .OnColumn("Status")
            .OnColumn("CreatedAt");
            
        // Covering index for common queries
        Create.Index("IX_HighVolumeData_Category_Status_Amount")
            .OnTable("HighVolumeData")
            .OnColumn("CategoryId")
            .OnColumn("Status")
            .OnColumn("Amount");
            
        // Partial index for active records only
        Execute.Sql(@"
            CREATE INDEX IX_HighVolumeData_Active_CreatedAt 
            ON HighVolumeData (CreatedAt) 
            WHERE Status = 'Active'");
            
        // Create summary table for faster aggregations
        Create.Table("DataSummary")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("SummaryDate").AsDate().NotNullable()
            .WithColumn("RecordCount").AsInt32().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(15, 2).NotNullable()
            .WithColumn("AvgAmount").AsDecimal(10, 2).NotNullable();
            
        Create.Index("UQ_DataSummary_Category_Status_Date")
            .OnTable("DataSummary")
            .OnColumn("CategoryId")
            .OnColumn("Status")
            .OnColumn("SummaryDate")
            .Unique();
            
        // Performance monitoring queries (for documentation)
        Execute.Sql(@"
            -- Performance monitoring examples:
            
            -- Check index usage
            -- EXPLAIN QUERY PLAN 
            -- SELECT * FROM HighVolumeData 
            -- WHERE CategoryId = 1 AND Status = 'Active';
            
            -- Analyze table statistics
            -- ANALYZE HighVolumeData;
            
            -- Check database size
            -- SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size();
            
            -- Vacuum to defragment
            -- VACUUM;
            
            -- Incremental vacuum in WAL mode
            -- PRAGMA wal_checkpoint(TRUNCATE);
            ");
    }

    public override void Down()
    {
        Delete.Table("DataSummary");
        Delete.Table("HighVolumeData");
    }
}
```

### Bulk Operations and Transactions

```csharp
public class SQLiteBulkOperations : Migration
{
    public override void Up()
    {
        Create.Table("BulkData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Value").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // Optimize for bulk inserts
        Execute.Sql("PRAGMA synchronous = OFF"); // Temporary for bulk operations
        Execute.Sql("PRAGMA journal_mode = MEMORY");
        Execute.Sql("PRAGMA temp_store = MEMORY");
        
        // Bulk insert using prepared statements (simulated with VALUES)
        Execute.Sql(@"
            INSERT INTO BulkData (Name, Value, CreatedAt) VALUES 
            ('Item 1', 100, datetime('now')),
            ('Item 2', 200, datetime('now')),
            ('Item 3', 300, datetime('now')),
            ('Item 4', 400, datetime('now')),
            ('Item 5', 500, datetime('now'))");
            
        // Restore normal settings after bulk operations
        Execute.Sql("PRAGMA synchronous = NORMAL");
        Execute.Sql("PRAGMA journal_mode = WAL");
        
        // Create view for aggregated data
        Execute.Sql(@"
            CREATE VIEW BulkDataSummary AS
            SELECT 
                COUNT(*) as TotalCount,
                SUM(Value) as TotalValue,
                AVG(Value) as AverageValue,
                MIN(Value) as MinValue,
                MAX(Value) as MaxValue,
                MIN(CreatedAt) as FirstCreated,
                MAX(CreatedAt) as LastCreated
            FROM BulkData");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS BulkDataSummary");
        Delete.Table("BulkData");
    }
}
```

## SQLite in Different Environments

### Development and Testing

```csharp
public class SQLiteDevelopmentSetup : Migration
{
    public override void Up()
    {
        // Set pragmas suitable for development
        Execute.Sql("PRAGMA foreign_keys = ON");      // Enforce constraints
        Execute.Sql("PRAGMA journal_mode = DELETE");  // Simple journaling
        Execute.Sql("PRAGMA synchronous = FULL");     // Maximum safety
        
        Create.Table("TestData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("TestValue").AsString(500).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Insert development seed data
        Insert.IntoTable("TestData")
            .Row(new { Name = "Development Test 1", TestValue = "Sample data for development" })
            .Row(new { Name = "Development Test 2", TestValue = "Another test record" })
            .Row(new { Name = "Development Test 3", TestValue = "Third test record", IsActive = false });
            
        // Create debug view
        Execute.Sql(@"
            CREATE VIEW TestDataDebug AS
            SELECT 
                Id,
                Name,
                TestValue,
                CASE 
                    WHEN IsActive = 1 THEN 'Active'
                    ELSE 'Inactive'
                END as StatusText,
                datetime(CreatedAt) as CreatedAtFormatted,
                LENGTH(TestValue) as ValueLength
            FROM TestData");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS TestDataDebug");
        Delete.Table("TestData");
    }
}
```

### Production Considerations

```csharp
public class SQLiteProductionSetup : Migration
{
    public override void Up()
    {
        // Production-optimized settings
        Execute.Sql("PRAGMA journal_mode = WAL");     // Best for concurrent reads
        Execute.Sql("PRAGMA synchronous = NORMAL");   // Good balance of safety/performance
        Execute.Sql("PRAGMA cache_size = -32000");    // 32MB cache for production
        Execute.Sql("PRAGMA temp_store = MEMORY");
        Execute.Sql("PRAGMA mmap_size = 134217728");  // 128MB memory mapping
        
        // Enable automatic checkpointing
        Execute.Sql("PRAGMA wal_autocheckpoint = 1000");
        
        Create.Table("ProductionData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EntityType").AsString(50).NotNullable()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("Data").AsCustom("TEXT").NotNullable()
            .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable();
            
        // Production indexes
        Create.Index("IX_ProductionData_EntityType_EntityId")
            .OnTable("ProductionData")
            .OnColumn("EntityType")
            .OnColumn("EntityId")
            .Unique();
            
        Create.Index("IX_ProductionData_CreatedAt")
            .OnTable("ProductionData")
            .OnColumn("CreatedAt");
            
        // Audit table for changes
        Create.Table("ProductionDataAudit")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("ProductionDataId").AsInt32().NotNullable()
            .WithColumn("Operation").AsString(10).NotNullable()
            .WithColumn("OldData").AsCustom("TEXT").Nullable()
            .WithColumn("NewData").AsCustom("TEXT").Nullable()
            .WithColumn("ChangedAt").AsDateTime().NotNullable();
            
        // Trigger for audit trail
        Execute.Sql(@"
            CREATE TRIGGER tr_ProductionData_Audit
            AFTER UPDATE ON ProductionData
            FOR EACH ROW
            BEGIN
                INSERT INTO ProductionDataAudit (
                    ProductionDataId, Operation, OldData, NewData, ChangedAt
                ) VALUES (
                    NEW.Id, 'UPDATE', OLD.Data, NEW.Data, datetime('now')
                );
                
                UPDATE ProductionData 
                SET UpdatedAt = datetime('now'), Version = Version + 1
                WHERE Id = NEW.Id;
            END");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER IF EXISTS tr_ProductionData_Audit");
        Delete.Table("ProductionDataAudit");
        Delete.Table("ProductionData");
    }
}
```

## SQLite Best Practices

### Schema Design Best Practices

```csharp
public class SQLiteBestPractices : Migration
{
    public override void Up()
    {
        // 1. Always use INTEGER PRIMARY KEY for better performance
        Create.Table("OptimizedTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity() // Maps to INTEGER PRIMARY KEY
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Value").AsDecimal(10, 2).NotNullable();
            
        // 2. Use appropriate data types
        Create.Table("DataTypeExample")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("IntegerField").AsInt32().NotNullable()     // Use INTEGER for better performance
            .WithColumn("TextField").AsString(255).NotNullable()    // TEXT with reasonable length
            .WithColumn("RealField").AsDouble().NotNullable()       // Use REAL for floating point
            .WithColumn("BlobField").AsBinary().Nullable()          // BLOB for binary data
            .WithColumn("BooleanField").AsBoolean().NotNullable();  // Stored as INTEGER 0/1
            
        // 3. Create indexes for foreign keys and frequently queried columns
        Create.Index("IX_OptimizedTable_Name")
            .OnTable("OptimizedTable")
            .OnColumn("Name");
            
        // 4. Use partial indexes to save space and improve performance
        Execute.Sql(@"
            CREATE INDEX IX_OptimizedTable_Value_High 
            ON OptimizedTable (Value) 
            WHERE Value > 100");
            
        // 5. Consider using WITHOUT ROWID for tables with composite primary keys
        Execute.Sql(@"
            CREATE TABLE CompositeKeyTable (
                Key1 INTEGER NOT NULL,
                Key2 TEXT NOT NULL,
                Data TEXT,
                PRIMARY KEY (Key1, Key2)
            ) WITHOUT ROWID");
            
        // 6. Use CHECK constraints for data validation
        Create.Table("ValidatedData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Age").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable();
            
        Execute.Sql(@"
            ALTER TABLE ValidatedData ADD CONSTRAINT CK_ValidatedData_Age 
            CHECK (Age >= 0 AND Age <= 150)");
            
        Execute.Sql(@"
            ALTER TABLE ValidatedData ADD CONSTRAINT CK_ValidatedData_Status 
            CHECK (Status IN ('Active', 'Inactive', 'Pending'))");
            
        Execute.Sql(@"
            ALTER TABLE ValidatedData ADD CONSTRAINT CK_ValidatedData_Amount 
            CHECK (Amount >= 0)");
    }

    public override void Down()
    {
        Delete.Table("ValidatedData");
        Execute.Sql("DROP TABLE IF EXISTS CompositeKeyTable");
        Delete.Table("DataTypeExample");
        Delete.Table("OptimizedTable");
    }
}
```

### Backup and Maintenance Strategies

```csharp
public class SQLiteMaintenanceStrategies : Migration
{
    public override void Up()
    {
        // Create maintenance tracking table
        Create.Table("MaintenanceLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("MaintenanceType").AsString(50).NotNullable()
            .WithColumn("StartedAt").AsDateTime().NotNullable()
            .WithColumn("CompletedAt").AsDateTime().Nullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Details").AsCustom("TEXT").Nullable();
            
        // Log this migration
        Insert.IntoTable("MaintenanceLog")
            .Row(new
            {
                MaintenanceType = "Schema Migration",
                StartedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                Status = "Completed",
                Details = "Created maintenance tracking infrastructure"
            });
            
        // Backup procedures (for documentation)
        Execute.Sql(@"
            -- SQLite backup and maintenance procedures:
            
            -- 1. Create backup using .backup command (SQLite CLI):
            -- .backup backup.db
            
            -- 2. Create backup using SQL:
            -- VACUUM INTO 'backup.db';
            
            -- 3. Regular maintenance tasks:
            -- PRAGMA integrity_check;  -- Check database integrity
            -- PRAGMA quick_check;      -- Quick integrity check
            -- ANALYZE;                 -- Update table statistics
            -- VACUUM;                  -- Defragment database
            -- PRAGMA wal_checkpoint(TRUNCATE);  -- Checkpoint WAL file
            
            -- 4. Monitor database size:
            -- SELECT 
            --     page_count * page_size as database_size,
            --     (page_count - freelist_count) * page_size as data_size,
            --     freelist_count * page_size as free_space
            -- FROM pragma_page_count(), pragma_page_size(), pragma_freelist_count();
            ");
    }

    public override void Down()
    {
        Delete.Table("MaintenanceLog");
    }
}
```

## Common SQLite Migration Patterns

### Migrating from Other Databases

```csharp
public class MigrateFromOtherDatabases : Migration
{
    public override void Up()
    {
        // When migrating from SQL Server/PostgreSQL/MySQL to SQLite:
        
        // 1. Handle auto-increment differences
        Create.Table("MigratedUsers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity() // AUTOINCREMENT in SQLite
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // 2. Handle GUID/UUID columns
        Create.Table("MigratedDocuments")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey() // Stored as TEXT in SQLite
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom("TEXT").Nullable();
            
        // 3. Handle decimal precision (SQLite stores as TEXT or REAL)
        Create.Table("MigratedFinancialData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Amount").AsDecimal(19, 4).NotNullable() // High precision stored as TEXT
            .WithColumn("Currency").AsString(3).NotNullable()
            .WithColumn("ExchangeRate").AsDouble().NotNullable(); // Lower precision as REAL
            
        // 4. Handle enum-like columns (use CHECK constraints)
        Create.Table("MigratedOrders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Priority").AsString(10).NotNullable();
            
        Execute.Sql(@"
            ALTER TABLE MigratedOrders ADD CONSTRAINT CK_MigratedOrders_Status 
            CHECK (Status IN ('Pending', 'Processing', 'Completed', 'Cancelled'))");
            
        Execute.Sql(@"
            ALTER TABLE MigratedOrders ADD CONSTRAINT CK_MigratedOrders_Priority 
            CHECK (Priority IN ('Low', 'Medium', 'High', 'Urgent'))");
            
        // 5. Handle complex data types with JSON (for newer SQLite versions)
        var supportsJson = CheckJsonSupport();
        if (supportsJson)
        {
            Create.Table("MigratedComplexData")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Data").AsCustom("TEXT").NotNullable() // JSON as TEXT
                .WithColumn("Metadata").AsCustom("TEXT").Nullable();
        }
        else
        {
            // Fallback: normalize complex data into separate tables
            Create.Table("MigratedComplexData")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("Description").AsString(500).Nullable();
                
            Create.Table("MigratedComplexDataAttributes")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("ParentId").AsInt32().NotNullable()
                .WithColumn("AttributeName").AsString(100).NotNullable()
                .WithColumn("AttributeValue").AsString(500).Nullable();
                
            Create.ForeignKey("FK_MigratedComplexDataAttributes_Parent")
                .FromTable("MigratedComplexDataAttributes").ForeignColumn("ParentId")
                .ToTable("MigratedComplexData").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);
        }
    }
    
    private bool CheckJsonSupport()
    {
        try
        {
            Execute.Sql("SELECT json('{}')");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override void Down()
    {
        if (Schema.Table("MigratedComplexDataAttributes").Exists())
        {
            Delete.ForeignKey("FK_MigratedComplexDataAttributes_Parent").OnTable("MigratedComplexDataAttributes");
            Delete.Table("MigratedComplexDataAttributes");
        }
        Delete.Table("MigratedComplexData");
        Delete.Table("MigratedOrders");
        Delete.Table("MigratedFinancialData");
        Delete.Table("MigratedDocuments");
        Delete.Table("MigratedUsers");
    }
}
```

## Troubleshooting Common SQLite Issues

### Common Problems and Solutions

```csharp
public class SQLiteTroubleshooting : Migration
{
    public override void Up()
    {
        // Problem 1: Database locked errors
        Execute.Sql("PRAGMA busy_timeout = 30000"); // 30 seconds timeout
        Execute.Sql("PRAGMA journal_mode = WAL");   // Reduces locking issues
        
        // Problem 2: Foreign key constraint violations
        Execute.Sql("PRAGMA foreign_keys = ON");    // Enable FK checking
        
        Create.Table("TroubleshootingParent")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
            
        Create.Table("TroubleshootingChild")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("ParentId").AsInt32().NotNullable()
            .WithColumn("ChildName").AsString(100).NotNullable();
            
        // Insert parent record first to avoid FK violations
        Insert.IntoTable("TroubleshootingParent")
            .Row(new { Name = "Parent 1" });
            
        // Problem 3: Data type mismatches
        // SQLite is flexible but can cause issues - use proper types
        Insert.IntoTable("TroubleshootingChild")
            .Row(new { ParentId = 1, ChildName = "Child 1" }); // Correct: integer
            
        // Create FK after data exists
        Create.ForeignKey("FK_TroubleshootingChild_Parent")
            .FromTable("TroubleshootingChild").ForeignColumn("ParentId")
            .ToTable("TroubleshootingParent").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
            
        // Problem 4: Performance issues with large datasets
        // Solution: Proper indexing and query optimization
        Create.Index("IX_TroubleshootingChild_ParentId")
            .OnTable("TroubleshootingChild")
            .OnColumn("ParentId");
            
        // Problem 5: Backup corruption
        // Solution: Regular integrity checks
        Execute.Sql(@"
            -- Regular maintenance queries:
            
            -- Check database integrity
            -- PRAGMA integrity_check;
            
            -- Quick check (faster)
            -- PRAGMA quick_check;
            
            -- Check foreign key constraints
            -- PRAGMA foreign_key_check;
            
            -- Optimize database
            -- VACUUM;
            
            -- Update statistics
            -- ANALYZE;
            ");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_TroubleshootingChild_Parent").OnTable("TroubleshootingChild");
        Delete.Table("TroubleshootingChild");
        Delete.Table("TroubleshootingParent");
    }
}
```

## See Also

- [Installation Guide](../installation.md)
- [Database Provider Comparison](./others.md)
- [PostgreSQL Provider](./postgresql.md)
- [MySQL Provider](./mysql.md)
- [SQL Server Provider](./sql-server.md)
- [Best Practices](../advanced/best-practices.md)
- [Troubleshooting](../advanced/edge-cases.md)