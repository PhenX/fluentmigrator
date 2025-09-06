# Data Operations

FluentMigrator provides powerful capabilities for manipulating data during migrations. This guide covers inserting, updating, deleting, and transforming data as part of your database schema evolution.

## Basic Data Operations

### Inserting Data

```csharp
public class BasicInsertOperations : Migration
{
    public override void Up()
    {
        // Create table first
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // Single row insert
        Insert.IntoTable("Users")
            .Row(new
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            
        // Multiple row insert
        Insert.IntoTable("Users")
            .Row(new { Name = "Jane Smith", Email = "jane.smith@example.com", IsActive = true, CreatedAt = DateTime.Now })
            .Row(new { Name = "Bob Johnson", Email = "bob.johnson@example.com", IsActive = true, CreatedAt = DateTime.Now })
            .Row(new { Name = "Alice Brown", Email = "alice.brown@example.com", IsActive = false, CreatedAt = DateTime.Now });
    }

    public override void Down()
    {
        Delete.FromTable("Users").AllRows();
        Delete.Table("Users");
    }
}
```

### Updating Data

```csharp
public class BasicUpdateOperations : Migration
{
    public override void Up()
    {
        // Update all rows
        Update.Table("Users")
            .Set(new { IsActive = true, UpdatedAt = DateTime.Now })
            .AllRows();
            
        // Update with WHERE condition
        Update.Table("Users")
            .Set(new { IsActive = false })
            .Where(new { Name = "John Doe" });
            
        // For complex SQL updates, see: Raw SQL (Scripts & Helpers)
        Execute.Sql("UPDATE Users SET LastLoginAt = GETDATE() WHERE Email LIKE '%@company.com'");
    }

    public override void Down()
    {
        // Restore original state if possible
        Update.Table("Users")
            .Set(new { IsActive = true })
            .AllRows();
    }
}
```

### Deleting Data

```csharp
public class BasicDeleteOperations : Migration
{
    public override void Up()
    {
        // Delete specific rows
        Delete.FromTable("Users")
            .Row(new { Name = "John Doe" });
            
        // Delete multiple rows with condition
        Delete.FromTable("Users")
            .Where(new { IsActive = false });
            
        // For complex SQL deletes, see: Raw SQL (Scripts & Helpers)
        Execute.Sql("DELETE FROM Users WHERE CreatedAt < '2020-01-01'");
    }

    public override void Down()
    {
        // Re-insert deleted data if needed (typically not possible in real scenarios)
        Insert.IntoTable("Users")
            .Row(new { Name = "John Doe", Email = "john.doe@example.com", IsActive = true });
    }
}
```

## Advanced Data Operations

### Data Migration Between Columns

```csharp
public class DataColumnMigration : Migration
{
    public override void Up()
    {
        // Add new column
        Alter.Table("Users")
            .AddColumn("FullName").AsString(200).Nullable();
            
        // Migrate data from existing columns - see Raw SQL guide for complex data migration patterns
        Execute.Sql("UPDATE Users SET FullName = COALESCE(FirstName + ' ' + LastName, FirstName, LastName) WHERE FullName IS NULL");
            
        // Make column not nullable after data migration
        Alter.Column("FullName").OnTable("Users")
            .AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("FullName").FromTable("Users");
    }
}
```

### Data Type Conversions

```csharp
public class DataTypeConversions : Migration
{
    public override void Up()
    {
        // Add new column with correct data type
        Alter.Table("Products")
            .AddColumn("PriceDecimal").AsDecimal(10, 2).Nullable();
            
        // Convert string price to decimal, handling invalid values
        IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
            UPDATE Products 
            SET PriceDecimal = TRY_CONVERT(DECIMAL(10,2), PriceString)
            WHERE ISNUMERIC(PriceString) = 1");

        IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
            UPDATE Products 
            SET PriceDecimal = CASE 
                WHEN PriceString ~ '^[0-9]+\.?[0-9]*$' THEN PriceString::DECIMAL(10,2)
                ELSE NULL 
            END");

        IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
            UPDATE Products 
            SET PriceDecimal = CASE 
                WHEN PriceString REGEXP '^[0-9]+\.?[0-9]*$' THEN CAST(PriceString AS DECIMAL(10,2))
                ELSE NULL 
            END");
        
        // Make new column not nullable after successful conversion
        Alter.Column("PriceDecimal").OnTable("Products")
            .AsDecimal(10, 2).NotNullable().WithDefaultValue(0.00m);
            
        // Remove old column
        Delete.Column("PriceString").FromTable("Products");
        
        // Rename new column to original name
        Rename.Column("PriceDecimal").OnTable("Products").To("Price");
    }

    public override void Down()
    {
        Rename.Column("Price").OnTable("Products").To("PriceDecimal");
        
        Alter.Table("Products")
            .AddColumn("PriceString").AsString(50).Nullable();
            
        Execute.Sql("UPDATE Products SET PriceString = CAST(PriceDecimal AS VARCHAR(50))");
        
        Delete.Column("PriceDecimal").FromTable("Products");
        
        Rename.Column("PriceString").OnTable("Products").To("Price");
    }
}
```

### Bulk Data Operations

For comprehensive bulk operations examples, see [Raw SQL (Scripts & Helpers)](../raw-sql-scripts.md#batch-processing-for-large-datasets).

```csharp
public class BulkDataOperations : Migration
{
    public override void Up()
    {
        // Basic bulk insert example - for complex bulk operations, use Execute.Sql
        Insert.IntoTable("Categories").Row(new { Name = "Electronics", IsActive = true });
        Insert.IntoTable("Categories").Row(new { Name = "Clothing", IsActive = true });
        
        // For complex bulk operations with joins and batch processing, see Raw SQL guide
        Execute.Sql("UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Electronics') WHERE CategoryName = 'Electronics'");
    }

    public override void Down()
    {
        Delete.FromTable("Categories").AllRows();
    }
}
```

## Working with Complex Data

### JSON Data Operations

```csharp
public class JsonDataOperations : Migration
{
    public override void Up()
    {
        // Create table with database-specific JSON column types
        IfDatabase(ProcessorIdConstants.SqlServer).Create.Table("UserProfiles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("ProfileData").AsCustom("NVARCHAR(MAX)").Nullable();
            
        IfDatabase(ProcessorIdConstants.Postgres).Create.Table("UserProfiles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("ProfileData").AsCustom("JSONB").Nullable();
            
        IfDatabase(ProcessorIdConstants.MySql).Create.Table("UserProfiles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("ProfileData").AsCustom("JSON").Nullable();
                
        // Insert JSON data (same for all databases)
        Insert.IntoTable("UserProfiles")
            .Row(new
            {
                UserId = 1,
                ProfileData = @"{
                    ""preferences"": {
                        ""theme"": ""dark"",
                        ""language"": ""en"",
                        ""notifications"": true
                    },
                    ""profile"": {
                        ""avatar"": ""avatar1.jpg"",
                        ""bio"": ""Software developer""
                    }
                }"
            });
            
        // Update JSON properties with database-specific syntax
        IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
            UPDATE UserProfiles 
            SET ProfileData = JSON_MODIFY(ProfileData, '$.preferences.theme', 'light')
            WHERE UserId = 1");

        IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
            UPDATE UserProfiles 
            SET ProfileData = ProfileData || '{""preferences"": {""theme"": ""light""}}'
            WHERE UserId = 1");
    }

    public override void Down()
    {
        if (Schema.Table("UserProfiles").Exists())
        {
            Delete.Table("UserProfiles");
        }
    }
}
```

### Working with Large Text Data

```csharp
public class LargeTextDataOperations : Migration
{
    public override void Up()
    {
        Create.Table("Documents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom(
                IfDatabase(ProcessorIdConstants.SqlServer) ? "NVARCHAR(MAX)" :
                IfDatabase(ProcessorIdConstants.Postgres) ? "TEXT" :
                IfDatabase(ProcessorIdConstants.MySql) ? "LONGTEXT" : "TEXT").Nullable()
            .WithColumn("ContentLength").AsInt32().Nullable();
            
        // Calculate and store content length
        Execute.Sql(@"
            UPDATE Documents 
            SET ContentLength = LEN(Content)
            WHERE Content IS NOT NULL");
            
        // Full-text indexing for search
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("CREATE FULLTEXT CATALOG DocumentsCatalog AS DEFAULT");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                ALTER TABLE Documents 
                ADD COLUMN SearchVector tsvector");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("DROP FULLTEXT INDEX ON Documents");
    IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Delete.Index("IX_Documents_SearchVector").OnTable("Documents");
            Delete.Column("SearchVector").FromTable("Documents");
    });
        
        Delete.Table("Documents");
    }
}
```

## Data Validation and Cleanup

### Data Quality Checks

```csharp
public class DataQualityChecks : Migration
{
    public override void Up()
    {
        // Check for duplicate email addresses
        var duplicateEmails = Execute.Sql(@"
            SELECT Email, COUNT(*) as Count
            FROM Users 
            GROUP BY Email 
            HAVING COUNT(*) > 1").Returns<dynamic>().ToList();
            
        if (duplicateEmails.Any())
        {
            // Log or handle duplicates
            foreach (var duplicate in duplicateEmails)
            {
                Execute.Sql($@"
                    -- Mark duplicate emails with a suffix
                    UPDATE Users 
                    SET Email = Email + '_' + CAST(Id AS VARCHAR(10))
                    WHERE Email = '{duplicate.Email}' 
                    AND Id NOT IN (SELECT MIN(Id) FROM Users WHERE Email = '{duplicate.Email}')");
            }
        }
        
        // Clean up invalid data
        Execute.Sql(@"
            UPDATE Users 
            SET Email = LOWER(TRIM(Email))
            WHERE Email != LOWER(TRIM(Email))");
            
        // Remove records with invalid email formats
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                DELETE FROM Users 
                WHERE Email NOT LIKE '%_@_%._%'");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                DELETE FROM Users 
                WHERE Email !~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'");
    }

    public override void Down()
    {
        // Data cleanup operations typically aren't reversed
    }
}
```

### Normalizing Data

```csharp
public class DataNormalization : Migration
{
    public override void Up()
    {
        // Extract categories into separate table
        Create.Table("Categories")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Insert unique categories
        Execute.Sql(@"
            INSERT INTO Categories (Name)
            SELECT DISTINCT CategoryName 
            FROM Products 
            WHERE CategoryName IS NOT NULL 
            AND CategoryName != ''");
            
        // Add CategoryId to Products table
        Alter.Table("Products")
            .AddColumn("CategoryId").AsInt32().Nullable();
            
        // Update Products with CategoryId
        Execute.Sql(@"
            UPDATE p 
            SET p.CategoryId = c.Id
            FROM Products p
            INNER JOIN Categories c ON p.CategoryName = c.Name");
            
        // Make CategoryId not null after data migration
        Alter.Column("CategoryId").OnTable("Products")
            .AsInt32().NotNullable();
            
        // Create foreign key
        Create.ForeignKey("FK_Products_Categories")
            .FromTable("Products").ForeignColumn("CategoryId")
            .ToTable("Categories").PrimaryColumn("Id");
            
        // Remove denormalized column
        Delete.Column("CategoryName").FromTable("Products");
    }

    public override void Down()
    {
        // Add back the denormalized column
        Alter.Table("Products")
            .AddColumn("CategoryName").AsString(100).Nullable();
            
        // Restore data from normalized table
        Execute.Sql(@"
            UPDATE p 
            SET p.CategoryName = c.Name
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id");
            
        // Remove foreign key and normalized structures
        Delete.ForeignKey("FK_Products_Categories").OnTable("Products");
        Delete.Column("CategoryId").FromTable("Products");
        Delete.Table("Categories");
    }
}
```

## Database-Specific Data Operations

### SQL Server Specific Operations

```csharp
public class SqlServerDataOperations : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                MERGE Users AS target
                USING (VALUES 
                    ('john@example.com', 'John Doe', 1),
                    ('jane@example.com', 'Jane Smith', 1)
                ) AS source (Email, Name, IsActive)
                ON target.Email = source.Email
                WHEN MATCHED THEN
                    UPDATE SET Name = source.Name, IsActive = source.IsActive
                WHEN NOT MATCHED THEN
                    INSERT (Email, Name, IsActive) VALUES (source.Email, source.Name, source.IsActive);");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Update.Table("Users").Set(new { Ranking = (int?)null }).AllRows();
    });
    }
}
```

### PostgreSQL Specific Operations

```csharp
public class PostgreSqlDataOperations : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                INSERT INTO Users (Email, Name, IsActive)
                VALUES 
                    ('john@example.com', 'John Doe', true),
                    ('jane@example.com', 'Jane Smith', true)
                ON CONFLICT (Email) DO UPDATE SET
                    Name = EXCLUDED.Name,
                    IsActive = EXCLUDED.IsActive,
                    UpdatedAt = NOW()");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql("UPDATE Users SET Tags = NULL, Ranking = NULL");
    }
}
```

### MySQL Specific Operations

```csharp
public class MySqlDataOperations : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
                INSERT INTO Users (Email, Name, IsActive)
                VALUES 
                    ('john@example.com', 'John Doe', true),
                    ('jane@example.com', 'Jane Smith', true)
                ON DUPLICATE KEY UPDATE
                    Name = VALUES(Name),
                    IsActive = VALUES(IsActive),
                    UpdatedAt = NOW()");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql("UPDATE Users SET FullName = NULL");
    }
}
```

## Performance Considerations

### Batch Processing for Large Datasets

```csharp
public class BatchProcessing : Migration
{
    public override void Up()
    {
        // Process data in batches to avoid lock escalation
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                DECLARE @BatchSize INT = 10000;
                DECLARE @RowsUpdated INT = @BatchSize;
                
                WHILE @RowsUpdated = @BatchSize
                BEGIN
                    UPDATE TOP (@BatchSize) Products 
                    SET UpdatedAt = GETDATE()
                    WHERE UpdatedAt IS NULL;
                    
                    SET @RowsUpdated = @@ROWCOUNT;
                END");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                DO $$
                DECLARE
                    batch_size INTEGER := 10000;
                    rows_updated INTEGER;
                BEGIN
                    LOOP
                        UPDATE Products 
                        SET UpdatedAt = NOW()
                        WHERE Id IN (
                            SELECT Id FROM Products 
                            WHERE UpdatedAt IS NULL 
                            LIMIT batch_size
                        );
                        
                        GET DIAGNOSTICS rows_updated = ROW_COUNT;
                        EXIT WHEN rows_updated = 0;
                    END LOOP;
                END $$");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Products SET UpdatedAt = NULL");
    }
}
```

### Using Temporary Tables for Complex Operations

```csharp
public class TemporaryTableOperations : Migration
{
    public override void Up()
    {
        // Create temporary table for staging data
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                CREATE TABLE #TempUserStats (
                    UserId INT,
                    OrderCount INT,
                    TotalAmount DECIMAL(10,2),
                    LastOrderDate DATETIME
                )");
        else
        {
            // For other databases, use regular temporary operations
            Execute.Sql(@"
                UPDATE Users 
                SET 
                    OrderCount = (SELECT COUNT(*) FROM Orders WHERE CustomerId = Users.Id),
                    TotalSpent = (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE CustomerId = Users.Id),
                    LastOrderDate = (SELECT MAX(OrderDate) FROM Orders WHERE CustomerId = Users.Id)");
        }
    }

    public override void Down()
    {
        Execute.Sql(@"
            UPDATE Users 
            SET OrderCount = NULL, TotalSpent = NULL, LastOrderDate = NULL");
    }
}
```

## Best Practices for Data Operations

### 1. Always Provide Rollback Strategy

```csharp
public class DataOperationsBestPractices : Migration
{
    public override void Up()
    {
        // Before making changes, backup critical data
        Execute.Sql(@"
            SELECT * INTO Users_Backup_" + DateTime.Now.ToString("yyyyMMdd") + @"
            FROM Users 
            WHERE IsActive = 0"); // Backup users we're about to delete
            
        // Perform the operation
        Delete.FromTable("Users").Where(new { IsActive = false });
    }

    public override void Down()
    {
        // Restore from backup
        var backupTable = "Users_Backup_" + DateTime.Now.ToString("yyyyMMdd");
        Execute.Sql($@"
            INSERT INTO Users (Name, Email, IsActive, CreatedAt)
            SELECT Name, Email, IsActive, CreatedAt 
            FROM {backupTable}");
    }
}
```

### 2. Validate Data Before Operations

```csharp
public class DataValidation : Migration
{
    public override void Up()
    {
        // Validate data before processing
        var invalidEmails = Execute.Sql(@"
            SELECT COUNT(*) 
            FROM Users 
            WHERE Email IS NULL OR Email = '' OR Email NOT LIKE '%@%'")
            .Returns<int>().FirstOrDefault();
            
        if (invalidEmails > 0)
        {
            throw new InvalidOperationException($"Found {invalidEmails} users with invalid emails. Please clean data first.");
        }
        
        // Safe to proceed with operation
        Execute.Sql("UPDATE Users SET Email = LOWER(Email)");
    }

    public override void Down()
    {
        // Reverse the operation
        Execute.Sql("UPDATE Users SET Email = Email"); // This is a no-op in practice
    }
}
```

### 3. Handle Large Datasets Efficiently

```csharp
public class EfficientDataHandling : Migration
{
    public override void Up()
    {
        // Check dataset size before operations
        var userCount = Execute.Sql("SELECT COUNT(*) FROM Users").Returns<int>().FirstOrDefault();
        
        if (userCount > 100000)
        {
            // Use batch processing for large datasets
                IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                    DECLARE @BatchSize INT = 5000;
                    WHILE EXISTS (SELECT 1 FROM Users WHERE UpdatedAt IS NULL)
                    BEGIN
                        UPDATE TOP (@BatchSize) Users 
                        SET UpdatedAt = GETDATE()
                        WHERE UpdatedAt IS NULL;
                        
                        WAITFOR DELAY '00:00:01'; -- Brief pause between batches
                    END");
        }
        else
        {
            // Small dataset - process all at once
            Execute.Sql("UPDATE Users SET UpdatedAt = GETDATE() WHERE UpdatedAt IS NULL");
        }
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET UpdatedAt = NULL");
    }
}
```

## Error Handling and Recovery

### Transactional Data Operations

```csharp
public class TransactionalDataOperations : Migration
{
    public override void Up()
    {
        // All operations within a migration are already transactional
        // But you can add explicit error checking
        try
        {
            Execute.Sql("UPDATE Users SET Status = 'Active' WHERE Status IS NULL");
            
            var updatedCount = Execute.Sql("SELECT @@ROWCOUNT").Returns<int>().FirstOrDefault();
            
            if (updatedCount == 0)
            {
                throw new InvalidOperationException("No rows were updated - this might indicate a problem");
            }
            
            Execute.Sql("INSERT INTO AuditLog (Action, Timestamp) VALUES ('UserStatusUpdate', GETDATE())");
        }
        catch (Exception ex)
        {
            // The transaction will be rolled back automatically
            throw new InvalidOperationException("Data operation failed: " + ex.Message, ex);
        }
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET Status = NULL WHERE Status = 'Active'");
        Execute.Sql("DELETE FROM AuditLog WHERE Action = 'UserStatusUpdate'");
    }
}
```

## Advanced SQL Operations

For comprehensive examples of advanced Execute.Sql operations including:
- Batch processing for large datasets
- Complex data transformations
- Database-specific optimizations  
- Error handling and validation
- Transaction control

See: [Raw SQL (Scripts & Helpers)](../raw-sql-scripts.md)

## See Also

- [Creating Tables](create-tables.md)
- [Altering Tables](alter-tables.md)
- [Managing Columns](../managing-columns.md)
- [Managing Indexes](../managing-indexes.md)
- [Working with Foreign Keys](../working-with-foreign-keys.md)
- [Best Practices](../advanced/best-practices.md)
- [Database Providers](../providers/)