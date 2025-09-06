# Advanced Migration Examples

This section provides comprehensive examples of advanced FluentMigrator scenarios, including complex data migrations, multi-database compatibility patterns, and real-world migration challenges.

## Advanced Data Transformation Scenarios

### Complex Data Type Migration

```csharp
[Migration(202401201000)]
public class ComplexDataTypeMigration : Migration
{
    public override void Up()
    {
        // Scenario: Migrating from loosely typed JSON storage to strongly typed columns
        
        // Create temporary staging table for data transformation
        Create.Table("UserPreferencesStaging")
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("NotificationEmail").AsBoolean().Nullable()
            .WithColumn("NotificationSMS").AsBoolean().Nullable()
            .WithColumn("Language").AsString(10).Nullable()
            .WithColumn("Theme").AsString(20).Nullable()
            .WithColumn("TimeZone").AsString(50).Nullable()
            .WithColumn("RawJson").AsString(4000).Nullable(); // Keep original for rollback
            
        // Extract structured data from JSON
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql(@"
                INSERT INTO UserPreferencesStaging (
                    UserId, NotificationEmail, NotificationSMS, Language, Theme, TimeZone, RawJson
                )
                SELECT 
                    UserId,
                    CASE WHEN JSON_VALUE(PreferencesJson, '$.notifications.email') = 'true' THEN 1 ELSE 0 END,
                    CASE WHEN JSON_VALUE(PreferencesJson, '$.notifications.sms') = 'true' THEN 1 ELSE 0 END,
                    JSON_VALUE(PreferencesJson, '$.language'),
                    JSON_VALUE(PreferencesJson, '$.theme'),
                    JSON_VALUE(PreferencesJson, '$.timezone'),
                    PreferencesJson
                FROM UserPreferences
                WHERE PreferencesJson IS NOT NULL");
        }
        else if (IfDatabase("Postgres"))
        {
            Execute.Sql(@"
                INSERT INTO UserPreferencesStaging (
                    UserId, NotificationEmail, NotificationSMS, Language, Theme, TimeZone, RawJson
                )
                SELECT 
                    UserId,
                    (PreferencesJson->>'notifications'->>'email')::boolean,
                    (PreferencesJson->>'notifications'->>'sms')::boolean,
                    PreferencesJson->>'language',
                    PreferencesJson->>'theme',
                    PreferencesJson->>'timezone',
                    PreferencesJson::text
                FROM UserPreferences
                WHERE PreferencesJson IS NOT NULL");
        }
        else
        {
            // Generic JSON parsing for other databases (simplified)
            Execute.Sql(@"
                INSERT INTO UserPreferencesStaging (UserId, RawJson)
                SELECT UserId, PreferencesJson
                FROM UserPreferences
                WHERE PreferencesJson IS NOT NULL");
        }
        
        // Validate data integrity
        var totalOriginal = Execute.Sql("SELECT COUNT(*) FROM UserPreferences WHERE PreferencesJson IS NOT NULL")
            .Returns<int>().FirstOrDefault();
        var totalMigrated = Execute.Sql("SELECT COUNT(*) FROM UserPreferencesStaging")
            .Returns<int>().FirstOrDefault();
            
        if (totalOriginal != totalMigrated)
        {
            throw new InvalidOperationException($"Data migration failed: {totalOriginal} original records, {totalMigrated} migrated");
        }
        
        // Replace original table with structured version
        Delete.Table("UserPreferences");
        Rename.Table("UserPreferencesStaging").To("UserPreferences");
        
        // Add constraints and indexes
        Create.Index("IX_UserPreferences_UserId")
            .OnTable("UserPreferences")
            .OnColumn("UserId")
            .Unique();
    }

    public override void Down()
    {
        // Restore JSON format from structured data
        Create.Table("UserPreferencesJson")
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("PreferencesJson").AsString(4000).Nullable();
            
        // Reconstruct JSON from structured data
        if (Schema.Table("UserPreferences").Column("RawJson").Exists())
        {
            // If we kept the raw JSON, use it
            Execute.Sql(@"
                INSERT INTO UserPreferencesJson (UserId, PreferencesJson)
                SELECT UserId, RawJson
                FROM UserPreferences
                WHERE RawJson IS NOT NULL");
        }
        else
        {
            // Reconstruct JSON from structured columns
            Execute.Sql(@"
                INSERT INTO UserPreferencesJson (UserId, PreferencesJson)
                SELECT 
                    UserId,
                    '{""notifications"":{""email"":' + 
                    CASE WHEN NotificationEmail = 1 THEN 'true' ELSE 'false' END + 
                    ',""sms"":' + 
                    CASE WHEN NotificationSMS = 1 THEN 'true' ELSE 'false' END + 
                    '},""language"":""' + COALESCE(Language, 'en') + 
                    '"",""theme"":""' + COALESCE(Theme, 'light') + 
                    '"",""timezone"":""' + COALESCE(TimeZone, 'UTC') + '""}'
                FROM UserPreferences");
        }
        
        Delete.Table("UserPreferences");
        Rename.Table("UserPreferencesJson").To("UserPreferences");
    }
}
```

### Multi-Step Data Migration with Dependencies

```csharp
[Migration(202401201100)]
public class MultiStepDataMigration : Migration
{
    public override void Up()
    {
        // Scenario: Normalizing denormalized order data with complex relationships
        
        // Step 1: Create normalized tables
        CreateNormalizedTables();
        
        // Step 2: Extract and deduplicate customers
        MigrateCustomers();
        
        // Step 3: Extract and deduplicate products
        MigrateProducts();
        
        // Step 4: Migrate orders with proper foreign keys
        MigrateOrders();
        
        // Step 5: Validate data integrity
        ValidateDataMigration();
        
        // Step 6: Drop old denormalized table
        Delete.Table("OrdersLegacy");
    }
    
    private void CreateNormalizedTables()
    {
        // First, rename the existing table to preserve data
        Rename.Table("Orders").To("OrdersLegacy");
        
        Create.Table("Customers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Phone").AsString(20).Nullable()
            .WithColumn("Address").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Table("Products")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("SKU").AsString(50).NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            .WithColumn("Category").AsString(100).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderNumber").AsString(50).NotNullable()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(15, 2).NotNullable()
            .WithColumn("Status").AsString(50).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Table("OrderItems")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("LineTotal").AsDecimal(15, 2).NotNullable();
    }
    
    private void MigrateCustomers()
    {
        // Extract unique customers from legacy orders
        Execute.Sql(@"
            INSERT INTO Customers (Name, Email, Phone, Address)
            SELECT DISTINCT
                COALESCE(CustomerName, 'Unknown Customer'),
                COALESCE(CustomerEmail, ''),
                CustomerPhone,
                CustomerAddress
            FROM OrdersLegacy
            WHERE CustomerName IS NOT NULL OR CustomerEmail IS NOT NULL");
            
        // Create mapping table for legacy to new IDs
        Create.Table("CustomerMapping")
            .WithColumn("LegacyKey").AsString(500).NotNullable()
            .WithColumn("NewCustomerId").AsInt32().NotNullable();
            
        Execute.Sql(@"
            INSERT INTO CustomerMapping (LegacyKey, NewCustomerId)
            SELECT 
                COALESCE(ol.CustomerName, '') + '|' + COALESCE(ol.CustomerEmail, ''),
                c.Id
            FROM (
                SELECT DISTINCT
                    COALESCE(CustomerName, 'Unknown Customer') as CustomerName,
                    COALESCE(CustomerEmail, '') as CustomerEmail
                FROM OrdersLegacy
                WHERE CustomerName IS NOT NULL OR CustomerEmail IS NOT NULL
            ) ol
            INNER JOIN Customers c ON 
                c.Name = ol.CustomerName AND 
                c.Email = ol.CustomerEmail");
    }
    
    private void MigrateProducts()
    {
        // Extract unique products from legacy orders
        Execute.Sql(@"
            INSERT INTO Products (Name, SKU, Price, Category)
            SELECT DISTINCT
                COALESCE(ProductName, 'Unknown Product'),
                COALESCE(ProductSKU, 'UNKNOWN-' + CAST(ROW_NUMBER() OVER (ORDER BY ProductName) AS VARCHAR)),
                COALESCE(ProductPrice, 0),
                ProductCategory
            FROM OrdersLegacy
            WHERE ProductName IS NOT NULL");
            
        // Create mapping table for legacy to new product IDs
        Create.Table("ProductMapping")
            .WithColumn("LegacyKey").AsString(500).NotNullable()
            .WithColumn("NewProductId").AsInt32().NotNullable();
            
        Execute.Sql(@"
            INSERT INTO ProductMapping (LegacyKey, NewProductId)
            SELECT 
                COALESCE(ol.ProductName, '') + '|' + COALESCE(ol.ProductSKU, ''),
                p.Id
            FROM (
                SELECT DISTINCT
                    COALESCE(ProductName, 'Unknown Product') as ProductName,
                    COALESCE(ProductSKU, '') as ProductSKU
                FROM OrdersLegacy
                WHERE ProductName IS NOT NULL
            ) ol
            INNER JOIN Products p ON 
                p.Name = ol.ProductName AND 
                (p.SKU = ol.ProductSKU OR (p.SKU LIKE 'UNKNOWN-%' AND ol.ProductSKU = ''))");
    }
    
    private void MigrateOrders()
    {
        // Create orders with proper customer references
        Execute.Sql(@"
            INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, TotalAmount, Status)
            SELECT DISTINCT
                ol.OrderNumber,
                cm.NewCustomerId,
                ol.OrderDate,
                ol.OrderTotal,
                COALESCE(ol.OrderStatus, 'Completed')
            FROM OrdersLegacy ol
            INNER JOIN CustomerMapping cm ON 
                cm.LegacyKey = COALESCE(ol.CustomerName, '') + '|' + COALESCE(ol.CustomerEmail, '')");
        
        // Create order items with proper product references
        Execute.Sql(@"
            INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
            SELECT 
                o.Id,
                pm.NewProductId,
                COALESCE(ol.Quantity, 1),
                COALESCE(ol.ProductPrice, 0),
                COALESCE(ol.Quantity, 1) * COALESCE(ol.ProductPrice, 0)
            FROM OrdersLegacy ol
            INNER JOIN Orders o ON o.OrderNumber = ol.OrderNumber
            INNER JOIN ProductMapping pm ON 
                pm.LegacyKey = COALESCE(ol.ProductName, '') + '|' + COALESCE(ol.ProductSKU, '')");
        
        // Add foreign key constraints
        Create.ForeignKey("FK_Orders_Customers")
            .FromTable("Orders").ForeignColumn("CustomerId")
            .ToTable("Customers").PrimaryColumn("Id");
            
        Create.ForeignKey("FK_OrderItems_Orders")
            .FromTable("OrderItems").ForeignColumn("OrderId")
            .ToTable("Orders").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
            
        Create.ForeignKey("FK_OrderItems_Products")
            .FromTable("OrderItems").ForeignColumn("ProductId")
            .ToTable("Products").PrimaryColumn("Id");
        
        // Clean up mapping tables
        Delete.Table("CustomerMapping");
        Delete.Table("ProductMapping");
    }
    
    private void ValidateDataMigration()
    {
        // Validate customer count
        var originalCustomerCount = Execute.Sql(@"
            SELECT COUNT(DISTINCT COALESCE(CustomerName, '') + '|' + COALESCE(CustomerEmail, ''))
            FROM OrdersLegacy").Returns<int>().FirstOrDefault();
        var newCustomerCount = Execute.Sql("SELECT COUNT(*) FROM Customers").Returns<int>().FirstOrDefault();
        
        if (originalCustomerCount != newCustomerCount)
        {
            Console.WriteLine($"Warning: Customer count mismatch - Original: {originalCustomerCount}, New: {newCustomerCount}");
        }
        
        // Validate order count
        var originalOrderCount = Execute.Sql("SELECT COUNT(DISTINCT OrderNumber) FROM OrdersLegacy")
            .Returns<int>().FirstOrDefault();
        var newOrderCount = Execute.Sql("SELECT COUNT(*) FROM Orders").Returns<int>().FirstOrDefault();
        
        if (originalOrderCount != newOrderCount)
        {
            throw new InvalidOperationException($"Order count mismatch - Original: {originalOrderCount}, New: {newOrderCount}");
        }
        
        // Validate total amounts match
        var originalTotal = Execute.Sql("SELECT SUM(COALESCE(OrderTotal, 0)) FROM OrdersLegacy")
            .Returns<decimal?>().FirstOrDefault() ?? 0;
        var newTotal = Execute.Sql("SELECT SUM(TotalAmount) FROM Orders")
            .Returns<decimal?>().FirstOrDefault() ?? 0;
            
        if (Math.Abs(originalTotal - newTotal) > 0.01m)
        {
            throw new InvalidOperationException($"Total amount mismatch - Original: {originalTotal}, New: {newTotal}");
        }
        
        Console.WriteLine("Data migration validation completed successfully");
        Console.WriteLine($"Migrated {newCustomerCount} customers and {newOrderCount} orders");
    }

    public override void Down()
    {
        // This is a complex rollback - in production, you might want to prevent this
        if (!Schema.Table("OrdersLegacy").Exists())
        {
            throw new InvalidOperationException("Cannot rollback: OrdersLegacy table not found");
        }
        
        Delete.Table("OrderItems");
        Delete.Table("Orders");
        Delete.Table("Products");
        Delete.Table("Customers");
        
        Rename.Table("OrdersLegacy").To("Orders");
    }
}
```

## Cross-Database Compatibility Patterns

### Universal Data Type Mapping

```csharp
[Migration(202401201200)]
public class UniversalDataTypeMigration : Migration
{
    public override void Up()
    {
        // Create a table that works across all major database providers
        Create.Table("UniversalDataTypes")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("Amount").AsDecimal(15, 2).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("BinaryData").AsBinary().Nullable()
            .WithColumn("UniqueId").AsGuid().NotNullable().WithDefaultValue(SystemMethods.NewGuid);
            
        // Add database-specific optimized columns
        AddDatabaseSpecificColumns();
        
        // Create indexes with database-specific optimizations
        CreateUniversalIndexes();
        
        // Add database-specific constraints
        AddDatabaseSpecificConstraints();
    }
    
    private void AddDatabaseSpecificColumns()
    {
        if (IfDatabase("SqlServer"))
        {
            Alter.Table("UniversalDataTypes")
                .AddColumn("XmlData").AsCustom("XML").Nullable()
                .AddColumn("HierarchyPath").AsCustom("HIERARCHYID").Nullable()
                .AddColumn("GeographicData").AsCustom("GEOGRAPHY").Nullable()
                .AddColumn("RowVersion").AsCustom("ROWVERSION").NotNullable();
        }
        else if (IfDatabase("Postgres"))
        {
            Alter.Table("UniversalDataTypes")
                .AddColumn("JsonData").AsCustom("JSONB").Nullable()
                .AddColumn("ArrayData").AsCustom("TEXT[]").Nullable()
                .AddColumn("NumericRange").AsCustom("NUMRANGE").Nullable()
                .AddColumn("NetworkAddress").AsCustom("INET").Nullable();
        }
        else if (IfDatabase("MySQL"))
        {
            Alter.Table("UniversalDataTypes")
                .AddColumn("JsonData").AsCustom("JSON").Nullable()
                .AddColumn("EnumValue").AsCustom("ENUM('small','medium','large')").Nullable()
                .AddColumn("SetValue").AsCustom("SET('red','green','blue')").Nullable()
                .AddColumn("GeometryData").AsCustom("GEOMETRY").Nullable();
        }
        else if (IfDatabase("SQLite"))
        {
            // SQLite stores everything as text, numbers, or blobs
            Alter.Table("UniversalDataTypes")
                .AddColumn("JsonText").AsString(4000).Nullable()
                .AddColumn("TagsText").AsString(1000).Nullable(); // Comma-separated
        }
        else if (IfDatabase("Oracle"))
        {
            Alter.Table("UniversalDataTypes")
                .AddColumn("XmlData").AsCustom("XMLType").Nullable()
                .AddColumn("LobData").AsCustom("CLOB").Nullable()
                .AddColumn("NumberData").AsCustom("NUMBER(38,10)").Nullable()
                .AddColumn("SpatialData").AsCustom("SDO_GEOMETRY").Nullable();
        }
    }
    
    private void CreateUniversalIndexes()
    {
        // Standard indexes that work on all databases
        Create.Index("IX_UniversalDataTypes_Name")
            .OnTable("UniversalDataTypes")
            .OnColumn("Name");
            
        Create.Index("IX_UniversalDataTypes_IsActive_CreatedAt")
            .OnTable("UniversalDataTypes")
            .OnColumn("IsActive")
            .OnColumn("CreatedAt");
            
        // Database-specific index optimizations
        if (IfDatabase("SqlServer"))
        {
            // Filtered index for active records only
            Execute.Sql(@"
                CREATE INDEX IX_UniversalDataTypes_Active_Filtered 
                ON UniversalDataTypes (CreatedAt DESC) 
                WHERE IsActive = 1");
                
            // Full-text index if available
            if (Schema.Table("UniversalDataTypes").Column("Description").Exists())
            {
                Execute.Sql("CREATE FULLTEXT INDEX ON UniversalDataTypes (Description)");
            }
        }
        else if (IfDatabase("Postgres"))
        {
            // Partial index for active records
            Execute.Sql(@"
                CREATE INDEX IX_UniversalDataTypes_Active_Partial 
                ON UniversalDataTypes (CreatedAt DESC) 
                WHERE IsActive = true");
                
            // GIN index for JSONB if column exists
            if (Schema.Table("UniversalDataTypes").Column("JsonData").Exists())
            {
                Execute.Sql(@"
                    CREATE INDEX IX_UniversalDataTypes_JsonData 
                    ON UniversalDataTypes USING GIN (JsonData)");
            }
        }
        else if (IfDatabase("MySQL"))
        {
            // MySQL-specific index hints
            Execute.Sql(@"
                CREATE INDEX IX_UniversalDataTypes_Composite 
                ON UniversalDataTypes (IsActive, CreatedAt DESC, Amount)");
                
            // Full-text index for text search
            if (Schema.Table("UniversalDataTypes").Column("Description").Exists())
            {
                Execute.Sql(@"
                    CREATE FULLTEXT INDEX IX_UniversalDataTypes_FullText 
                    ON UniversalDataTypes (Name, Description)");
            }
        }
    }
    
    private void AddDatabaseSpecificConstraints()
    {
        // Universal constraints
        Create.Index("UQ_UniversalDataTypes_Name_Unique")
            .OnTable("UniversalDataTypes")
            .OnColumn("Name")
            .OnColumn("CreatedAt")
            .Unique();
            
        // Database-specific constraints
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql(@"
                ALTER TABLE UniversalDataTypes 
                ADD CONSTRAINT CK_UniversalDataTypes_Amount_Positive 
                CHECK (Amount >= 0)");
        }
        else if (IfDatabase("Postgres"))
        {
            Execute.Sql(@"
                ALTER TABLE UniversalDataTypes 
                ADD CONSTRAINT CK_UniversalDataTypes_Amount_Positive 
                CHECK (Amount >= 0)");
        }
        else if (IfDatabase("MySQL"))
        {
            Execute.Sql(@"
                ALTER TABLE UniversalDataTypes 
                ADD CONSTRAINT CK_UniversalDataTypes_Amount_Positive 
                CHECK (Amount >= 0)");
        }
        else if (IfDatabase("SQLite"))
        {
            // SQLite check constraints
            Execute.Sql(@"
                CREATE TRIGGER TR_UniversalDataTypes_Amount_Check
                BEFORE INSERT ON UniversalDataTypes
                FOR EACH ROW
                WHEN NEW.Amount < 0
                BEGIN
                    SELECT RAISE(ABORT, 'Amount must be non-negative');
                END");
        }
    }

    public override void Down()
    {
        // Clean up database-specific objects
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP FULLTEXT INDEX ON UniversalDataTypes");
        }
        else if (IfDatabase("SQLite"))
        {
            Execute.Sql("DROP TRIGGER IF EXISTS TR_UniversalDataTypes_Amount_Check");
        }
        
        Delete.Table("UniversalDataTypes");
    }
}
```

### Database Feature Detection and Fallbacks

```csharp
[Migration(202401201300)]
public class FeatureDetectionMigration : Migration
{
    public override void Up()
    {
        Create.Table("FeatureDetectionTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Data").AsString(1000).Nullable();
            
        // Attempt to use advanced features with fallbacks
        AddJsonSupport();
        AddFullTextSearch();
        AddComputedColumns();
        AddPartitioning();
    }
    
    private void AddJsonSupport()
    {
        Console.WriteLine("Adding JSON support...");
        
        if (IfDatabase("SqlServer"))
        {
            // Check for SQL Server 2016+ JSON support
            try
            {
                Execute.Sql("SELECT JSON_VALUE('{}', '$')");
                Alter.Table("FeatureDetectionTable")
                    .AddColumn("JsonData").AsCustom("NVARCHAR(MAX)").Nullable();
                    
                Execute.Sql(@"
                    ALTER TABLE FeatureDetectionTable 
                    ADD CONSTRAINT CK_FeatureDetectionTable_ValidJson 
                    CHECK (JsonData IS NULL OR ISJSON(JsonData) = 1)");
                    
                Console.WriteLine("SQL Server JSON support enabled");
            }
            catch
            {
                // Fallback to XML
                Alter.Table("FeatureDetectionTable")
                    .AddColumn("XmlData").AsCustom("XML").Nullable();
                Console.WriteLine("SQL Server JSON not available, using XML");
            }
        }
        else if (IfDatabase("Postgres"))
        {
            // PostgreSQL has excellent JSON support
            Alter.Table("FeatureDetectionTable")
                .AddColumn("JsonData").AsCustom("JSONB").Nullable();
                
            Execute.Sql(@"
                CREATE INDEX IX_FeatureDetectionTable_JsonData 
                ON FeatureDetectionTable USING GIN (JsonData)");
                
            Console.WriteLine("PostgreSQL JSONB support enabled");
        }
        else if (IfDatabase("MySQL"))
        {
            // Check MySQL version for JSON support
            try
            {
                Execute.Sql("SELECT JSON_EXTRACT('{}', '$')");
                Alter.Table("FeatureDetectionTable")
                    .AddColumn("JsonData").AsCustom("JSON").Nullable();
                Console.WriteLine("MySQL JSON support enabled");
            }
            catch
            {
                // Fallback to TEXT
                Alter.Table("FeatureDetectionTable")
                    .AddColumn("JsonText").AsCustom("TEXT").Nullable();
                Console.WriteLine("MySQL JSON not available, using TEXT");
            }
        }
        else
        {
            // Generic fallback
            Alter.Table("FeatureDetectionTable")
                .AddColumn("JsonText").AsString(4000).Nullable();
            Console.WriteLine("Using generic text storage for JSON");
        }
    }
    
    private void AddFullTextSearch()
    {
        Console.WriteLine("Adding full-text search support...");
        
        try
        {
            if (IfDatabase("SqlServer"))
            {
                // Check if full-text is available
                var fullTextAvailable = Execute.Sql(@"
                    SELECT SERVERPROPERTY('IsFullTextInstalled')")
                    .Returns<int?>().FirstOrDefault() == 1;
                    
                if (fullTextAvailable)
                {
                    Execute.Sql("CREATE FULLTEXT CATALOG SearchCatalog AS DEFAULT");
                    Execute.Sql(@"
                        CREATE FULLTEXT INDEX ON FeatureDetectionTable (Name, Data)
                        KEY INDEX PK_FeatureDetectionTable ON SearchCatalog");
                    Console.WriteLine("SQL Server full-text search enabled");
                }
                else
                {
                    CreateBasicTextIndex();
                }
            }
            else if (IfDatabase("Postgres"))
            {
                Execute.Sql(@"
                    ALTER TABLE FeatureDetectionTable 
                    ADD COLUMN SearchVector tsvector");
                    
                Execute.Sql(@"
                    CREATE INDEX IX_FeatureDetectionTable_Search 
                    ON FeatureDetectionTable USING GIN (SearchVector)");
                    
                Execute.Sql(@"
                    CREATE TRIGGER TR_FeatureDetectionTable_SearchVector
                    BEFORE INSERT OR UPDATE ON FeatureDetectionTable
                    FOR EACH ROW
                    EXECUTE FUNCTION tsvector_update_trigger(SearchVector, 'pg_catalog.english', Name, Data)");
                    
                Console.WriteLine("PostgreSQL full-text search enabled");
            }
            else if (IfDatabase("MySQL"))
            {
                Execute.Sql(@"
                    CREATE FULLTEXT INDEX IX_FeatureDetectionTable_FullText 
                    ON FeatureDetectionTable (Name, Data)");
                Console.WriteLine("MySQL full-text search enabled");
            }
            else
            {
                CreateBasicTextIndex();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Full-text search not available: {ex.Message}");
            CreateBasicTextIndex();
        }
    }
    
    private void CreateBasicTextIndex()
    {
        Create.Index("IX_FeatureDetectionTable_Name_Text")
            .OnTable("FeatureDetectionTable")
            .OnColumn("Name");
        Console.WriteLine("Using basic text indexing");
    }
    
    private void AddComputedColumns()
    {
        Console.WriteLine("Adding computed columns...");
        
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql(@"
                ALTER TABLE FeatureDetectionTable 
                ADD NameLength AS (LEN(Name)) PERSISTED");
                
            Execute.Sql(@"
                ALTER TABLE FeatureDetectionTable 
                ADD NameUpper AS (UPPER(Name))");
                
            Console.WriteLine("SQL Server computed columns added");
        }
        else if (IfDatabase("Postgres"))
        {
            // PostgreSQL doesn't have computed columns, use generated columns (12+)
            try
            {
                Execute.Sql(@"
                    ALTER TABLE FeatureDetectionTable 
                    ADD COLUMN NameLength INTEGER GENERATED ALWAYS AS (LENGTH(Name)) STORED");
                Console.WriteLine("PostgreSQL generated columns added");
            }
            catch
            {
                // Fallback to regular column with trigger
                Alter.Table("FeatureDetectionTable")
                    .AddColumn("NameLength").AsInt32().Nullable();
                    
                Execute.Sql(@"
                    CREATE OR REPLACE FUNCTION update_name_length()
                    RETURNS TRIGGER AS $$
                    BEGIN
                        NEW.NameLength = LENGTH(NEW.Name);
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql");
                    
                Execute.Sql(@"
                    CREATE TRIGGER TR_FeatureDetectionTable_NameLength
                    BEFORE INSERT OR UPDATE ON FeatureDetectionTable
                    FOR EACH ROW
                    EXECUTE FUNCTION update_name_length()");
                    
                Console.WriteLine("PostgreSQL trigger-based computed columns added");
            }
        }
        else
        {
            // Fallback to regular columns
            Alter.Table("FeatureDetectionTable")
                .AddColumn("NameLength").AsInt32().Nullable();
            Console.WriteLine("Using regular columns instead of computed columns");
        }
    }
    
    private void AddPartitioning()
    {
        Console.WriteLine("Adding partitioning support...");
        
        if (IfDatabase("SqlServer"))
        {
            try
            {
                // Create partition function and scheme
                Execute.Sql(@"
                    CREATE PARTITION FUNCTION PF_FeatureDetection (INT)
                    AS RANGE RIGHT FOR VALUES (1000, 2000, 3000)");
                    
                Execute.Sql(@"
                    CREATE PARTITION SCHEME PS_FeatureDetection
                    AS PARTITION PF_FeatureDetection
                    ALL TO ([PRIMARY])");
                    
                // Note: In real scenario, you'd create the table with partition scheme
                Console.WriteLine("SQL Server partitioning scheme created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Server partitioning not available: {ex.Message}");
            }
        }
        else if (IfDatabase("Postgres"))
        {
            try
            {
                // Create partitioned table (would be done during table creation)
                // This is just an example of range partitioning
                Execute.Sql(@"
                    CREATE TABLE FeatureDetectionPartitioned (
                        LIKE FeatureDetectionTable INCLUDING ALL
                    ) PARTITION BY RANGE (Id)");
                    
                Execute.Sql(@"
                    CREATE TABLE FeatureDetectionPartitioned_1_1000 
                    PARTITION OF FeatureDetectionPartitioned 
                    FOR VALUES FROM (1) TO (1000)");
                    
                Console.WriteLine("PostgreSQL partitioning created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PostgreSQL partitioning not available: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Partitioning not supported on this database");
        }
    }

    public override void Down()
    {
        // Clean up database-specific features
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP PARTITION SCHEME PS_FeatureDetection");
            Execute.Sql("DROP PARTITION FUNCTION PF_FeatureDetection");
            Execute.Sql("DROP FULLTEXT INDEX ON FeatureDetectionTable");
            Execute.Sql("DROP FULLTEXT CATALOG SearchCatalog");
        }
        else if (IfDatabase("Postgres"))
        {
            Execute.Sql("DROP TABLE IF EXISTS FeatureDetectionPartitioned_1_1000");
            Execute.Sql("DROP TABLE IF EXISTS FeatureDetectionPartitioned");
            Execute.Sql("DROP TRIGGER IF EXISTS TR_FeatureDetectionTable_NameLength ON FeatureDetectionTable");
            Execute.Sql("DROP FUNCTION IF EXISTS update_name_length()");
            Execute.Sql("DROP TRIGGER IF EXISTS TR_FeatureDetectionTable_SearchVector ON FeatureDetectionTable");
        }
        
        Delete.Table("FeatureDetectionTable");
    }
}
```

## Performance-Critical Migration Patterns

### Large Table Migration with Minimal Downtime

```csharp
[Migration(202401201400)]
public class LargeTableMigration : Migration
{
    private const int BATCH_SIZE = 10000;
    
    public override void Up()
    {
        // Scenario: Adding a column to a table with millions of records
        
        Console.WriteLine("Starting large table migration with minimal downtime...");
        
        // Step 1: Add column as nullable (non-blocking in most databases)
        Alter.Table("LargeUserTable")
            .AddColumn("EmailVerified").AsBoolean().Nullable();
            
        // Step 2: Create index concurrently if supported
        CreateIndexConcurrently();
        
        // Step 3: Update data in batches to avoid long locks
        UpdateDataInBatches();
        
        // Step 4: Add NOT NULL constraint with default (potentially blocking)
        MakeColumnNotNullable();
        
        Console.WriteLine("Large table migration completed");
    }
    
    private void CreateIndexConcurrently()
    {
        if (IfDatabase("Postgres"))
        {
            // PostgreSQL supports concurrent index creation
            Execute.Sql(@"
                CREATE INDEX CONCURRENTLY IX_LargeUserTable_EmailVerified 
                ON LargeUserTable (EmailVerified)");
        }
        else if (IfDatabase("SqlServer"))
        {
            // SQL Server online index creation
            Execute.Sql(@"
                CREATE INDEX IX_LargeUserTable_EmailVerified 
                ON LargeUserTable (EmailVerified) 
                WITH (ONLINE = ON)");
        }
        else
        {
            // Standard index creation
            Create.Index("IX_LargeUserTable_EmailVerified")
                .OnTable("LargeUserTable")
                .OnColumn("EmailVerified");
        }
    }
    
    private void UpdateDataInBatches()
    {
        Console.WriteLine("Updating data in batches...");
        
        // Get total count for progress tracking
        var totalRecords = Execute.Sql("SELECT COUNT(*) FROM LargeUserTable WHERE EmailVerified IS NULL")
            .Returns<int>().FirstOrDefault();
            
        if (totalRecords == 0)
        {
            Console.WriteLine("No records to update");
            return;
        }
        
        Console.WriteLine($"Updating {totalRecords:N0} records in batches of {BATCH_SIZE:N0}");
        
        int batchNumber = 1;
        int totalProcessed = 0;
        
        if (IfDatabase("SqlServer"))
        {
            UpdateSqlServerBatches(ref batchNumber, ref totalProcessed, totalRecords);
        }
        else if (IfDatabase("Postgres"))
        {
            UpdatePostgresBatches(ref batchNumber, ref totalProcessed, totalRecords);
        }
        else if (IfDatabase("MySQL"))
        {
            UpdateMySqlBatches(ref batchNumber, ref totalProcessed, totalRecords);
        }
        else
        {
            UpdateGenericBatches(ref batchNumber, ref totalProcessed, totalRecords);
        }
        
        Console.WriteLine($"Data update completed: {totalProcessed:N0} records processed");
    }
    
    private void UpdateSqlServerBatches(ref int batchNumber, ref int totalProcessed, int totalRecords)
    {
        Execute.Sql($@"
            DECLARE @BatchSize INT = {BATCH_SIZE};
            DECLARE @RowsUpdated INT = @BatchSize;
            DECLARE @TotalProcessed INT = 0;
            DECLARE @BatchNumber INT = 1;
            
            WHILE @RowsUpdated = @BatchSize
            BEGIN
                UPDATE TOP (@BatchSize) LargeUserTable 
                SET EmailVerified = CASE 
                    WHEN Email IS NOT NULL AND Email LIKE '%@%' THEN 1 
                    ELSE 0 
                END
                WHERE EmailVerified IS NULL;
                
                SET @RowsUpdated = @@ROWCOUNT;
                SET @TotalProcessed = @TotalProcessed + @RowsUpdated;
                
                PRINT 'Batch ' + CAST(@BatchNumber AS VARCHAR) + 
                      ': Updated ' + CAST(@RowsUpdated AS VARCHAR) + 
                      ' records. Total: ' + CAST(@TotalProcessed AS VARCHAR) + '/{totalRecords}';
                
                SET @BatchNumber = @BatchNumber + 1;
                
                -- Brief pause to avoid overwhelming the system
                WAITFOR DELAY '00:00:01';
            END");
    }
    
    private void UpdatePostgresBatches(ref int batchNumber, ref int totalProcessed, int totalRecords)
    {
        Execute.Sql($@"
            DO $$
            DECLARE
                batch_size INTEGER := {BATCH_SIZE};
                rows_updated INTEGER;
                total_processed INTEGER := 0;
                batch_number INTEGER := 1;
            BEGIN
                LOOP
                    UPDATE LargeUserTable 
                    SET EmailVerified = CASE 
                        WHEN Email IS NOT NULL AND Email LIKE '%@%' THEN true 
                        ELSE false 
                    END
                    WHERE Id IN (
                        SELECT Id FROM LargeUserTable 
                        WHERE EmailVerified IS NULL 
                        ORDER BY Id 
                        LIMIT batch_size
                    );
                    
                    GET DIAGNOSTICS rows_updated = ROW_COUNT;
                    total_processed := total_processed + rows_updated;
                    
                    RAISE NOTICE 'Batch %: Updated % records. Total: %/{totalRecords}', 
                        batch_number, rows_updated, total_processed;
                    
                    batch_number := batch_number + 1;
                    
                    EXIT WHEN rows_updated = 0;
                    
                    -- Brief pause
                    PERFORM pg_sleep(1);
                END LOOP;
            END $$");
    }
    
    private void UpdateMySqlBatches(ref int batchNumber, ref int totalProcessed, int totalRecords)
    {
        int processed = 0;
        
        do
        {
            var rowsUpdated = Execute.Sql($@"
                UPDATE LargeUserTable 
                SET EmailVerified = CASE 
                    WHEN Email IS NOT NULL AND Email LIKE '%@%' THEN 1 
                    ELSE 0 
                END
                WHERE EmailVerified IS NULL 
                LIMIT {BATCH_SIZE}").Returns<int>().FirstOrDefault();
                
            processed += rowsUpdated;
            totalProcessed += rowsUpdated;
            
            Console.WriteLine($"Batch {batchNumber}: Updated {rowsUpdated:N0} records. Total: {totalProcessed:N0}/{totalRecords:N0}");
            
            batchNumber++;
            
            if (rowsUpdated == 0) break;
            
            // Brief pause between batches
            System.Threading.Thread.Sleep(1000);
            
        } while (processed > 0);
    }
    
    private void UpdateGenericBatches(ref int batchNumber, ref int totalProcessed, int totalRecords)
    {
        // Generic approach for databases without batch-specific syntax
        var recordsToProcess = Execute.Sql($@"
            SELECT Id FROM LargeUserTable 
            WHERE EmailVerified IS NULL 
            ORDER BY Id 
            LIMIT {BATCH_SIZE}").Returns<int>().ToList();
            
        while (recordsToProcess.Any())
        {
            var idList = string.Join(",", recordsToProcess);
            
            Execute.Sql($@"
                UPDATE LargeUserTable 
                SET EmailVerified = CASE 
                    WHEN Email IS NOT NULL AND Email LIKE '%@%' THEN 1 
                    ELSE 0 
                END
                WHERE Id IN ({idList})");
                
            totalProcessed += recordsToProcess.Count;
            Console.WriteLine($"Batch {batchNumber}: Updated {recordsToProcess.Count:N0} records. Total: {totalProcessed:N0}/{totalRecords:N0}");
            
            batchNumber++;
            
            // Get next batch
            recordsToProcess = Execute.Sql($@"
                SELECT Id FROM LargeUserTable 
                WHERE EmailVerified IS NULL 
                ORDER BY Id 
                LIMIT {BATCH_SIZE}").Returns<int>().ToList();
                
            System.Threading.Thread.Sleep(1000);
        }
    }
    
    private void MakeColumnNotNullable()
    {
        Console.WriteLine("Making column NOT NULL...");
        
        // Ensure no NULL values remain
        var nullCount = Execute.Sql("SELECT COUNT(*) FROM LargeUserTable WHERE EmailVerified IS NULL")
            .Returns<int>().FirstOrDefault();
            
        if (nullCount > 0)
        {
            throw new InvalidOperationException($"Cannot make column NOT NULL: {nullCount} NULL values remain");
        }
        
        // Make column NOT NULL with default value
        Alter.Column("EmailVerified").OnTable("LargeUserTable")
            .AsBoolean().NotNullable().WithDefaultValue(false);
            
        Console.WriteLine("Column is now NOT NULL");
    }

    public override void Down()
    {
        Console.WriteLine("Rolling back large table migration...");
        
        Delete.Index("IX_LargeUserTable_EmailVerified").OnTable("LargeUserTable");
        Delete.Column("EmailVerified").FromTable("LargeUserTable");
        
        Console.WriteLine("Rollback completed");
    }
}
```

## Real-World Integration Scenarios

### Legacy System Integration

```csharp
[Migration(202401201500)]
public class LegacySystemIntegration : Migration
{
    public override void Up()
    {
        // Scenario: Integrating with legacy system that has poor data quality
        
        Console.WriteLine("Starting legacy system integration...");
        
        // Create staging tables for data cleanup
        CreateStagingTables();
        
        // Import and clean legacy data
        ImportLegacyData();
        
        // Create clean, normalized tables
        CreateNormalizedTables();
        
        // Migrate cleaned data to normalized structure
        MigrateCleanedData();
        
        // Create integration views for legacy system compatibility
        CreateCompatibilityViews();
        
        Console.WriteLine("Legacy system integration completed");
    }
    
    private void CreateStagingTables()
    {
        Create.Table("LegacyCustomersStaging")
            .WithColumn("OriginalId").AsString(50).NotNullable()
            .WithColumn("CustomerData").AsString(4000).NotNullable() // Raw CSV-like data
            .WithColumn("ImportedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("ProcessedAt").AsDateTime().Nullable()
            .WithColumn("HasErrors").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("ErrorMessages").AsString(2000).Nullable()
            .WithColumn("CleanedName").AsString(200).Nullable()
            .WithColumn("CleanedEmail").AsString(255).Nullable()
            .WithColumn("CleanedPhone").AsString(20).Nullable();
            
        Create.Table("DataQualityLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TableName").AsString(100).NotNullable()
            .WithColumn("RecordId").AsString(100).NotNullable()
            .WithColumn("IssueType").AsString(50).NotNullable()
            .WithColumn("IssueDescription").AsString(500).NotNullable()
            .WithColumn("OriginalValue").AsString(500).Nullable()
            .WithColumn("SuggestedValue").AsString(500).Nullable()
            .WithColumn("DetectedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }
    
    private void ImportLegacyData()
    {
        // This would typically load from CSV files or external database
        // For demo purposes, inserting sample problematic data
        
        var problemData = new[]
        {
            new { OriginalId = "CUST001", CustomerData = "John Doe,john.doe@email.com,555-1234,123 Main St" },
            new { OriginalId = "CUST002", CustomerData = "jane smith,JANE@COMPANY.COM,5551234567,456 Oak Ave" },
            new { OriginalId = "CUST003", CustomerData = "Bob,invalid-email,phone,incomplete address" },
            new { OriginalId = "CUST004", CustomerData = "Alice Johnson,alice@test.com,(555) 123-4567,789 Pine St, Apt 2" }
        };
        
        foreach (var data in problemData)
        {
            Insert.IntoTable("LegacyCustomersStaging")
                .Row(data);
        }
        
        // Clean and parse the data
        CleanLegacyData();
    }
    
    private void CleanLegacyData()
    {
        Console.WriteLine("Cleaning legacy data...");
        
        // Parse CSV-like data and clean it
        var stagingRecords = Execute.Sql("SELECT OriginalId, CustomerData FROM LegacyCustomersStaging WHERE ProcessedAt IS NULL")
            .Returns<dynamic>().ToList();
            
        foreach (var record in stagingRecords)
        {
            var originalId = (string)record.OriginalId;
            var customerData = (string)record.CustomerData;
            
            var parts = customerData.Split(',');
            
            var cleanedName = "";
            var cleanedEmail = "";
            var cleanedPhone = "";
            var hasErrors = false;
            var errorMessages = new List<string>();
            
            // Clean name
            if (parts.Length > 0)
            {
                cleanedName = CleanName(parts[0]);
                if (string.IsNullOrWhiteSpace(cleanedName))
                {
                    errorMessages.Add("Invalid or missing name");
                    hasErrors = true;
                }
            }
            
            // Clean email
            if (parts.Length > 1)
            {
                cleanedEmail = CleanEmail(parts[1]);
                if (!IsValidEmail(cleanedEmail))
                {
                    LogDataQualityIssue("LegacyCustomersStaging", originalId, "InvalidEmail", 
                        $"Invalid email: {parts[1]}", parts[1], "");
                    errorMessages.Add($"Invalid email: {parts[1]}");
                    hasErrors = true;
                    cleanedEmail = "";
                }
            }
            
            // Clean phone
            if (parts.Length > 2)
            {
                cleanedPhone = CleanPhone(parts[2]);
                if (string.IsNullOrWhiteSpace(cleanedPhone))
                {
                    LogDataQualityIssue("LegacyCustomersStaging", originalId, "InvalidPhone", 
                        $"Invalid phone: {parts[2]}", parts[2], "");
                    errorMessages.Add($"Invalid phone: {parts[2]}");
                    hasErrors = true;
                }
            }
            
            // Update staging record
            Execute.Sql($@"
                UPDATE LegacyCustomersStaging 
                SET CleanedName = '{cleanedName.Replace("'", "''")}',
                    CleanedEmail = '{cleanedEmail}',
                    CleanedPhone = '{cleanedPhone}',
                    HasErrors = {(hasErrors ? 1 : 0)},
                    ErrorMessages = '{string.Join("; ", errorMessages).Replace("'", "''")}',
                    ProcessedAt = GETDATE()
                WHERE OriginalId = '{originalId}'");
        }
        
        var totalRecords = stagingRecords.Count;
        var errorRecords = Execute.Sql("SELECT COUNT(*) FROM LegacyCustomersStaging WHERE HasErrors = 1")
            .Returns<int>().FirstOrDefault();
            
        Console.WriteLine($"Data cleaning completed: {totalRecords} records processed, {errorRecords} with errors");
    }
    
    private string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        
        // Proper case conversion
        var words = name.Trim().ToLower().Split(' ');
        var cleanedWords = words.Select(word => 
            string.IsNullOrEmpty(word) ? "" : 
            char.ToUpper(word[0]) + (word.Length > 1 ? word.Substring(1) : ""));
            
        return string.Join(" ", cleanedWords.Where(w => !string.IsNullOrEmpty(w)));
    }
    
    private string CleanEmail(string email)
    {
        return email?.Trim().ToLower() ?? "";
    }
    
    private string CleanPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        
        // Extract only digits
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        // Format as (XXX) XXX-XXXX for 10-digit numbers
        if (digits.Length == 10)
        {
            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }
        else if (digits.Length == 11 && digits.StartsWith("1"))
        {
            var numberPart = digits.Substring(1);
            return $"({numberPart.Substring(0, 3)}) {numberPart.Substring(3, 3)}-{numberPart.Substring(6, 4)}";
        }
        
        return ""; // Invalid phone number
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    private void LogDataQualityIssue(string tableName, string recordId, string issueType, 
        string description, string originalValue, string suggestedValue)
    {
        Insert.IntoTable("DataQualityLog")
            .Row(new
            {
                TableName = tableName,
                RecordId = recordId,
                IssueType = issueType,
                IssueDescription = description,
                OriginalValue = originalValue,
                SuggestedValue = suggestedValue
            });
    }
    
    private void CreateNormalizedTables()
    {
        Create.Table("Customers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("LegacyId").AsString(50).NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Email").AsString(255).Nullable()
            .WithColumn("Phone").AsString(20).Nullable()
            .WithColumn("DataQualityScore").AsInt32().NotNullable().WithDefaultValue(100)
            .WithColumn("ImportedFrom").AsString(100).NotNullable().WithDefaultValue("LegacySystem")
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("LastVerifiedAt").AsDateTime().Nullable();
            
        Create.Index("UQ_Customers_LegacyId")
            .OnTable("Customers")
            .OnColumn("LegacyId")
            .Unique();
            
        Create.Index("IX_Customers_Email")
            .OnTable("Customers")
            .OnColumn("Email");
    }
    
    private void MigrateCleanedData()
    {
        Execute.Sql(@"
            INSERT INTO Customers (LegacyId, Name, Email, Phone, DataQualityScore)
            SELECT 
                OriginalId,
                CleanedName,
                NULLIF(CleanedEmail, ''),
                NULLIF(CleanedPhone, ''),
                CASE WHEN HasErrors = 1 THEN 60 ELSE 100 END
            FROM LegacyCustomersStaging
            WHERE CleanedName IS NOT NULL AND CleanedName != ''");
            
        var migratedCount = Execute.Sql("SELECT COUNT(*) FROM Customers")
            .Returns<int>().FirstOrDefault();
            
        Console.WriteLine($"Migrated {migratedCount} customers to normalized table");
    }
    
    private void CreateCompatibilityViews()
    {
        // Create views that match legacy system expectations
        Execute.Sql(@"
            CREATE VIEW LegacyCustomerView AS
            SELECT 
                LegacyId as customer_id,
                UPPER(Name) as customer_name,
                UPPER(COALESCE(Email, '')) as email_address,
                COALESCE(Phone, '') as phone_number,
                FORMAT(CreatedAt, 'MM/dd/yyyy') as date_created,
                CASE 
                    WHEN DataQualityScore >= 90 THEN 'A'
                    WHEN DataQualityScore >= 70 THEN 'B'
                    ELSE 'C'
                END as quality_grade
            FROM Customers");
            
        // Create stored procedures for legacy system integration
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql(@"
                CREATE PROCEDURE sp_GetLegacyCustomer
                    @customer_id VARCHAR(50)
                AS
                BEGIN
                    SELECT * FROM LegacyCustomerView 
                    WHERE customer_id = @customer_id
                END");
        }
    }

    public override void Down()
    {
        // Clean up in reverse order
        Execute.Sql("DROP VIEW IF EXISTS LegacyCustomerView");
        
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP PROCEDURE IF EXISTS sp_GetLegacyCustomer");
        }
        
        Delete.Table("Customers");
        Delete.Table("DataQualityLog");
        Delete.Table("LegacyCustomersStaging");
    }
}
```

These advanced examples demonstrate real-world migration scenarios including complex data transformations, cross-database compatibility, performance optimization, and legacy system integration patterns that developers commonly encounter in production environments.

## See Also

- [Basic Examples](basic.md)
- [Real-World Use Cases](real-world.md)
- [Best Practices](../advanced/best-practices.md)
- [Database Providers](../providers/)
- [Common Operations](../operations/)