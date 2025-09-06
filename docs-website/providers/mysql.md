# MySQL

MySQL is a popular open-source relational database management system. FluentMigrator provides comprehensive support for MySQL, including MySQL-specific data types, storage engines, and features.

## Getting Started with MySQL

### Installation

Install the MySQL provider package:

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.MySql

# For Package Manager Console
Install-Package FluentMigrator.Runner.MySql
```

### Basic Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddMySql()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

### Connection String Examples

```csharp
// Basic connection
"Server=localhost;Database=myapp;Uid=myuser;Pwd=mypassword;"

// With SSL
"Server=localhost;Database=myapp;Uid=myuser;Pwd=mypassword;SslMode=Required;"

// Connection pooling
"Server=localhost;Database=myapp;Uid=myuser;Pwd=mypassword;Pooling=true;MinPoolSize=0;MaxPoolSize=100;"

// Cloud providers (like Amazon RDS)
"Server=myinstance.cluster-xyz.us-east-1.rds.amazonaws.com;Database=myapp;Uid=myuser;Pwd=mypassword;SslMode=Required;"
```

## MySQL-Specific Data Types

### Basic MySQL Data Types

```csharp
public class MySqlDataTypes : Migration
{
    public override void Up()
    {
        Create.Table("MySqlTypeExamples")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()

            // Integer types
            .WithColumn("TinyIntValue").AsCustom("TINYINT").NotNullable()
            .WithColumn("SmallIntValue").AsCustom("SMALLINT").NotNullable()
            .WithColumn("MediumIntValue").AsCustom("MEDIUMINT").NotNullable()
            .WithColumn("IntValue").AsInt32().NotNullable()
            .WithColumn("BigIntValue").AsInt64().NotNullable()

            // Unsigned integers
            .WithColumn("UnsignedTinyInt").AsCustom("TINYINT UNSIGNED").NotNullable()
            .WithColumn("UnsignedSmallInt").AsCustom("SMALLINT UNSIGNED").NotNullable()
            .WithColumn("UnsignedMediumInt").AsCustom("MEDIUMINT UNSIGNED").NotNullable()
            .WithColumn("UnsignedInt").AsCustom("INT UNSIGNED").NotNullable()
            .WithColumn("UnsignedBigInt").AsCustom("BIGINT UNSIGNED").NotNullable()

            // Decimal and floating point
            .WithColumn("DecimalValue").AsDecimal(10, 2).NotNullable()
            .WithColumn("FloatValue").AsFloat().NotNullable()
            .WithColumn("DoubleValue").AsDouble().NotNullable()

            // String types
            .WithColumn("CharValue").AsFixedLengthString(10).NotNullable()
            .WithColumn("VarCharValue").AsString(255).NotNullable()
            .WithColumn("TinyTextValue").AsCustom("TINYTEXT").Nullable()
            .WithColumn("TextValue").AsCustom("TEXT").Nullable()
            .WithColumn("MediumTextValue").AsCustom("MEDIUMTEXT").Nullable()
            .WithColumn("LongTextValue").AsCustom("LONGTEXT").Nullable()

            // Binary types
            .WithColumn("BinaryValue").AsCustom("BINARY(16)").Nullable()
            .WithColumn("VarBinaryValue").AsCustom("VARBINARY(255)").Nullable()
            .WithColumn("TinyBlobValue").AsCustom("TINYBLOB").Nullable()
            .WithColumn("BlobValue").AsBinary().Nullable()
            .WithColumn("MediumBlobValue").AsCustom("MEDIUMBLOB").Nullable()
            .WithColumn("LongBlobValue").AsCustom("LONGBLOB").Nullable()

            // Date and time types
            .WithColumn("DateValue").AsDate().Nullable()
            .WithColumn("TimeValue").AsTime().Nullable()
            .WithColumn("DateTimeValue").AsDateTime().NotNullable()
            .WithColumn("TimestampValue").AsCustom("TIMESTAMP").NotNullable()
            .WithColumn("YearValue").AsCustom("YEAR").Nullable()

            // Boolean
            .WithColumn("BooleanValue").AsBoolean().NotNullable()

            // JSON (MySQL 5.7+)
            .WithColumn("JsonValue").AsCustom("JSON").Nullable();
    }

    public override void Down()
    {
        Delete.Table("MySqlTypeExamples");
    }
}
```

### MySQL Enum and Set Types

```csharp
public class MySqlEnumSetTypes : Migration
{
    public override void Up()
    {
        Create.Table("ProductCatalog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()

            // ENUM type for single selection
            .WithColumn("Size").AsCustom("ENUM('XS','S','M','L','XL','XXL')").NotNullable()
            .WithColumn("Status").AsCustom("ENUM('draft','published','archived')").NotNullable()
                .WithDefaultValue(RawSql.Insert("'draft'"))
            .WithColumn("Priority").AsCustom("ENUM('low','medium','high','urgent')").NotNullable()
                .WithDefaultValue(RawSql.Insert("'medium'"))

            // SET type for multiple selections
            .WithColumn("Features").AsCustom("SET('waterproof','breathable','insulated','reflective')").Nullable()
            .WithColumn("Colors").AsCustom("SET('red','blue','green','black','white','yellow')").Nullable()
            .WithColumn("Categories").AsCustom("SET('outdoor','sports','casual','formal','work')").Nullable()

            .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("ProductCatalog");
    }
}
```

## MySQL Storage Engines

### Specifying Storage Engines

```csharp
public class MySqlStorageEngines : Migration
{
    public override void Up()
    {
        // InnoDB table (default, ACID compliant, supports foreign keys)
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable();

        Execute.Sql("ALTER TABLE Orders ENGINE = InnoDB");

        // MyISAM table (fast for read-heavy operations, no foreign keys)
        Create.Table("SearchIndex")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("DocumentId").AsInt32().NotNullable()
            .WithColumn("Keywords").AsCustom("TEXT").NotNullable()
            .WithColumn("RelevanceScore").AsFloat().NotNullable();

        Execute.Sql("ALTER TABLE SearchIndex ENGINE = MyISAM");

        // Memory table (stored in RAM, very fast but volatile)
        Create.Table("SessionData")
            .WithColumn("SessionId").AsString(128).NotNullable().PrimaryKey()
            .WithColumn("UserId").AsInt32().Nullable()
            .WithColumn("Data").AsCustom("TEXT").Nullable()
            .WithColumn("LastAccessed").AsCustom("TIMESTAMP").NotNullable()
                .WithDefaultValue(SystemMethods.CurrentDateTime);

        Execute.Sql("ALTER TABLE SessionData ENGINE = MEMORY");

        // Archive table (compressed storage for historical data)
        Create.Table("AuditLogArchive")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TableName").AsString(100).NotNullable()
            .WithColumn("Action").AsString(20).NotNullable()
            .WithColumn("RecordId").AsInt32().NotNullable()
            .WithColumn("ChangedAt").AsDateTime().NotNullable();

        Execute.Sql("ALTER TABLE AuditLogArchive ENGINE = ARCHIVE");
    }

    public override void Down()
    {
        Delete.Table("AuditLogArchive");
        Delete.Table("SessionData");
        Delete.Table("SearchIndex");
        Delete.Table("Orders");
    }
}
```

## MySQL Indexes and Full-Text Search

### MySQL Index Types

```csharp
public class MySqlIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Articles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom("TEXT").NotNullable()
            .WithColumn("Summary").AsString(1000).Nullable()
            .WithColumn("AuthorId").AsInt32().NotNullable()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("Tags").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable()
            .WithColumn("Coordinates").AsCustom("POINT").Nullable();

        // Standard B-tree indexes
        Create.Index("IX_Articles_AuthorId")
            .OnTable("Articles")
            .OnColumn("AuthorId");

        Create.Index("IX_Articles_CategoryId_CreatedAt")
            .OnTable("Articles")
            .OnColumn("CategoryId")
            .OnColumn("CreatedAt");

        // Full-text index for search
        Execute.Sql(@"
            CREATE FULLTEXT INDEX IX_Articles_FullText
            ON Articles (Title, Content, Summary)");

        // Hash index (only supported by MEMORY engine)
        Create.Table("LookupCache")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("CacheKey").AsString(100).NotNullable()
            .WithColumn("CacheValue").AsCustom("TEXT").Nullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable();

        Execute.Sql("ALTER TABLE LookupCache ENGINE = MEMORY");
        Execute.Sql("CREATE INDEX IX_LookupCache_Key USING HASH ON LookupCache (CacheKey)");

        // Spatial index (for geometric data)
        if (Schema.Table("Articles").Column("Coordinates").Exists())
        {
            Execute.Sql("CREATE SPATIAL INDEX IX_Articles_Location ON Articles (Coordinates)");
        }

        // Prefix index for large text columns
        Execute.Sql("CREATE INDEX IX_Articles_Tags_Prefix ON Articles (Tags(100))");
    }

    public override void Down()
    {
        Delete.Table("LookupCache");
        Delete.Table("Articles");
    }
}
```

### Full-Text Search Implementation

```csharp
public class MySqlFullTextSearch : Migration
{
    public override void Up()
    {
        Create.Table("Documents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom("LONGTEXT").NotNullable()
            .WithColumn("Tags").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();

        // Create full-text index
        Execute.Sql(@"
            CREATE FULLTEXT INDEX IX_Documents_Search
            ON Documents (Title, Content, Tags)");

        // Insert sample data
        Insert.IntoTable("Documents")
            .Row(new
            {
                Title = "MySQL Full-Text Search Guide",
                Content = "This document explains how to implement full-text search in MySQL databases using FULLTEXT indexes and MATCH AGAINST syntax.",
                Tags = "mysql, search, database, fulltext",
                CreatedAt = DateTime.Now
            })
            .Row(new
            {
                Title = "Database Optimization Techniques",
                Content = "Learn various techniques to optimize your MySQL database performance including indexing, query optimization, and configuration tuning.",
                Tags = "mysql, optimization, performance, indexing",
                CreatedAt = DateTime.Now.AddDays(-1)
            });

        // Example search queries (for documentation purposes)
        Execute.Sql(@"
            -- Natural language search
            -- SELECT Id, Title, MATCH(Title, Content, Tags) AGAINST('mysql database') AS relevance
            -- FROM Documents
            -- WHERE MATCH(Title, Content, Tags) AGAINST('mysql database')
            -- ORDER BY relevance DESC;

            -- Boolean search
            -- SELECT Id, Title
            -- FROM Documents
            -- WHERE MATCH(Title, Content, Tags) AGAINST('+mysql +optimization -indexing' IN BOOLEAN MODE);

            -- Search with query expansion
            -- SELECT Id, Title
            -- FROM Documents
            -- WHERE MATCH(Title, Content, Tags) AGAINST('database' WITH QUERY EXPANSION);
            ");
    }

    public override void Down()
    {
        Delete.Table("Documents");
    }
}
```

## MySQL Partitioning

### Table Partitioning Strategies

```csharp
public class MySqlPartitioning : Migration
{
    public override void Up()
    {
        // Range partitioning by date
        Create.Table("SalesData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("SaleDate").AsDate().NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable();

        Execute.Sql(@"
            ALTER TABLE SalesData
            PARTITION BY RANGE (YEAR(SaleDate)) (
                PARTITION p2020 VALUES LESS THAN (2021),
                PARTITION p2021 VALUES LESS THAN (2022),
                PARTITION p2022 VALUES LESS THAN (2023),
                PARTITION p2023 VALUES LESS THAN (2024),
                PARTITION p2024 VALUES LESS THAN (2025),
                PARTITION pFuture VALUES LESS THAN MAXVALUE
            )");

        // Hash partitioning for even distribution
        Create.Table("UserSessions")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("SessionToken").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable();

        Execute.Sql(@"
            ALTER TABLE UserSessions
            PARTITION BY HASH(UserId)
            PARTITIONS 8");

        // List partitioning by region
        Create.Table("CustomerData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Region").AsString(20).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();

        Execute.Sql(@"
            ALTER TABLE CustomerData
            PARTITION BY LIST COLUMNS(Region) (
                PARTITION pNorthAmerica VALUES IN ('USA', 'Canada', 'Mexico'),
                PARTITION pEurope VALUES IN ('UK', 'Germany', 'France', 'Spain'),
                PARTITION pAsia VALUES IN ('Japan', 'China', 'India', 'Korea'),
                PARTITION pOther VALUES IN (DEFAULT)
            )");
    }

    public override void Down()
    {
        Delete.Table("CustomerData");
        Delete.Table("UserSessions");
        Delete.Table("SalesData");
    }
}
```

## MySQL JSON Support

### Working with JSON Data (MySQL 5.7+)

```csharp
public class MySqlJsonSupport : Migration
{
    public override void Up()
    {
        Create.Table("UserProfiles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("ProfileData").AsCustom("JSON").NotNullable()
            .WithColumn("Preferences").AsCustom("JSON").Nullable()
            .WithColumn("Metadata").AsCustom("JSON").Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable();

        // Create indexes on JSON columns
        Execute.Sql(@"
            CREATE INDEX IX_UserProfiles_ProfileData_Name
            ON UserProfiles ((CAST(ProfileData->>'$.name' AS CHAR(100))))");

        Execute.Sql(@"
            CREATE INDEX IX_UserProfiles_ProfileData_Age
            ON UserProfiles ((CAST(ProfileData->>'$.age' AS UNSIGNED)))");

        // Insert sample JSON data
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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

        // Create a view for easier JSON querying
        Execute.Sql(@"
            CREATE VIEW UserProfileView AS
            SELECT
                Id,
                UserId,
                JSON_UNQUOTE(JSON_EXTRACT(ProfileData, '$.name')) AS Name,
                JSON_UNQUOTE(JSON_EXTRACT(ProfileData, '$.email')) AS Email,
                CAST(JSON_EXTRACT(ProfileData, '$.age') AS UNSIGNED) AS Age,
                JSON_UNQUOTE(JSON_EXTRACT(ProfileData, '$.address.city')) AS City,
                JSON_UNQUOTE(JSON_EXTRACT(Preferences, '$.theme')) AS Theme,
                CreatedAt,
                UpdatedAt
            FROM UserProfiles");

        // Example queries for JSON data (for documentation)
        Execute.Sql(@"
            -- Query examples for JSON data:

            -- Select users by age
            -- SELECT * FROM UserProfiles
            -- WHERE CAST(ProfileData->>'$.age' AS UNSIGNED) > 25;

            -- Select users by hobby
            -- SELECT * FROM UserProfiles
            -- WHERE JSON_CONTAINS(ProfileData, '""gaming""', '$.hobbies');

            -- Update JSON field
            -- UPDATE UserProfiles
            -- SET ProfileData = JSON_SET(ProfileData, '$.age', 31)
            -- WHERE UserId = 1;

            -- Add new JSON property
            -- UPDATE UserProfiles
            -- SET ProfileData = JSON_INSERT(ProfileData, '$.lastLogin', NOW())
            -- WHERE UserId = 1;
            ");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS UserProfileView");
        Delete.Table("UserProfiles");
    }
}
```

## MySQL Performance Optimization

### Query Cache and Configuration

```csharp
public class MySqlPerformanceOptimization : Migration
{
    public override void Up()
    {
        // Create tables optimized for performance
        Create.Table("HighVolumeTransactions")
            .WithColumn("Id").AsInt64().NotNullable().PrimaryKey().Identity()
            .WithColumn("TransactionType").AsString(20).NotNullable()
            .WithColumn("Amount").AsDecimal(15, 2).NotNullable()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsCustom("TIMESTAMP").NotNullable()
                .WithDefaultValue(SystemMethods.CurrentDateTime);

        // Optimize for InnoDB
        Execute.Sql("ALTER TABLE HighVolumeTransactions ENGINE = InnoDB");
        Execute.Sql("ALTER TABLE HighVolumeTransactions ROW_FORMAT = COMPRESSED");

        // Create optimized indexes
        Create.Index("IX_HighVolumeTransactions_UserId_CreatedAt")
            .OnTable("HighVolumeTransactions")
            .OnColumn("UserId")
            .OnColumn("CreatedAt");

        Create.Index("IX_HighVolumeTransactions_Type_Amount")
            .OnTable("HighVolumeTransactions")
            .OnColumn("TransactionType")
            .OnColumn("Amount");

        // Create summary table for faster reporting
        Create.Table("TransactionSummary")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("TransactionType").AsString(20).NotNullable()
            .WithColumn("TransactionDate").AsDate().NotNullable()
            .WithColumn("TransactionCount").AsInt32().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(15, 2).NotNullable()
            .WithColumn("AvgAmount").AsDecimal(15, 2).NotNullable();

        // Create unique index to prevent duplicates
        Create.Index("UQ_TransactionSummary_User_Type_Date")
            .OnTable("TransactionSummary")
            .OnColumn("UserId")
            .OnColumn("TransactionType")
            .OnColumn("TransactionDate")
            .Unique();

        // Performance optimization queries (for documentation)
        Execute.Sql(@"
            -- Performance optimization examples:

            -- Use EXPLAIN to analyze query performance
            -- EXPLAIN SELECT * FROM HighVolumeTransactions
            -- WHERE UserId = 1 AND CreatedAt > '2023-01-01';

            -- Use covering index for better performance
            -- SELECT TransactionType, SUM(Amount)
            -- FROM HighVolumeTransactions
            -- WHERE UserId = 1
            -- GROUP BY TransactionType;

            -- Optimize INSERT performance with bulk operations
            -- INSERT INTO HighVolumeTransactions (TransactionType, Amount, UserId) VALUES
            -- ('purchase', 100.00, 1),
            -- ('refund', -25.00, 1),
            -- ('purchase', 75.50, 2);
            ");
    }

    public override void Down()
    {
        Delete.Table("TransactionSummary");
        Delete.Table("HighVolumeTransactions");
    }
}
```

### MySQL Triggers and Stored Procedures

```csharp
public class MySqlTriggersAndProcedures : Migration
{
    public override void Up()
    {
        Create.Table("Products")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            .WithColumn("StockQuantity").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable();

        Create.Table("ProductAudit")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("OldPrice").AsDecimal(10, 2).Nullable()
            .WithColumn("NewPrice").AsDecimal(10, 2).Nullable()
            .WithColumn("OldQuantity").AsInt32().Nullable()
            .WithColumn("NewQuantity").AsInt32().Nullable()
            .WithColumn("ChangeType").AsString(20).NotNullable()
            .WithColumn("ChangedAt").AsDateTime().NotNullable();

        // Create trigger for automatic UpdatedAt timestamp
        Execute.Sql(@"
            CREATE TRIGGER tr_Products_UpdatedAt
            BEFORE UPDATE ON Products
            FOR EACH ROW
            SET NEW.UpdatedAt = NOW()");

        // Create audit trigger
        Execute.Sql(@"
            CREATE TRIGGER tr_Products_Audit
            AFTER UPDATE ON Products
            FOR EACH ROW
            INSERT INTO ProductAudit (
                ProductId, OldPrice, NewPrice, OldQuantity, NewQuantity,
                ChangeType, ChangedAt
            ) VALUES (
                NEW.Id, OLD.Price, NEW.Price, OLD.StockQuantity, NEW.StockQuantity,
                'UPDATE', NOW()
            )");

        // Create stored procedure for complex operations
        Execute.Sql(@"
            DELIMITER //
            CREATE PROCEDURE sp_UpdateProductStock(
                IN p_ProductId INT,
                IN p_QuantityChange INT,
                OUT p_NewQuantity INT
            )
            BEGIN
                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    ROLLBACK;
                    RESIGNAL;
                END;

                START TRANSACTION;

                UPDATE Products
                SET StockQuantity = StockQuantity + p_QuantityChange,
                    UpdatedAt = NOW()
                WHERE Id = p_ProductId;

                SELECT StockQuantity INTO p_NewQuantity
                FROM Products
                WHERE Id = p_ProductId;

                COMMIT;
            END//
            DELIMITER ;");

        // Create function for calculations
        Execute.Sql(@"
            DELIMITER //
            CREATE FUNCTION fn_CalculateTotalValue(p_ProductId INT)
            RETURNS DECIMAL(15,2)
            READS SQL DATA
            DETERMINISTIC
            BEGIN
                DECLARE v_Total DECIMAL(15,2);

                SELECT Price * StockQuantity INTO v_Total
                FROM Products
                WHERE Id = p_ProductId;

                RETURN COALESCE(v_Total, 0);
            END//
            DELIMITER ;");
    }

    public override void Down()
    {
        Execute.Sql("DROP FUNCTION IF EXISTS fn_CalculateTotalValue");
        Execute.Sql("DROP PROCEDURE IF EXISTS sp_UpdateProductStock");
        Execute.Sql("DROP TRIGGER IF EXISTS tr_Products_Audit");
        Execute.Sql("DROP TRIGGER IF EXISTS tr_Products_UpdatedAt");
        Delete.Table("ProductAudit");
        Delete.Table("Products");
    }
}
```

## MySQL Best Practices

### Character Sets and Collations

```csharp
public class MySqlCharacterSets : Migration
{
    public override void Up()
    {
        // Create database with UTF8MB4 (full UTF-8 support including emojis)
        Execute.Sql("ALTER DATABASE `" + Context.Database + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        Create.Table("InternationalContent")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsCustom("TEXT").NotNullable()
            .WithColumn("Language").AsString(10).NotNullable()
            .WithColumn("Author").AsString(100).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();

        // Specify character set and collation for table
        Execute.Sql(@"
            ALTER TABLE InternationalContent
            CONVERT TO CHARACTER SET utf8mb4
            COLLATE utf8mb4_unicode_ci");

        // Create table with case-insensitive collation
        Create.Table("CaseInsensitiveData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable();

        Execute.Sql(@"
            ALTER TABLE CaseInsensitiveData
            MODIFY Username VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
            MODIFY Email VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL");

        // Insert multilingual sample data
        Insert.IntoTable("InternationalContent")
            .Row(new
            {
                Title = "Hello World",
                Content = "English content with emoji 👋",
                Language = "en",
                Author = "John Doe",
                CreatedAt = DateTime.Now
            })
            .Row(new
            {
                Title = "こんにちは世界",
                Content = "Japanese content with kanji characters and emoji 🌍",
                Language = "ja",
                Author = "田中太郎",
                CreatedAt = DateTime.Now
            })
            .Row(new
            {
                Title = "Hola Mundo",
                Content = "Contenido en español with accents and emoji 🇪🇸",
                Language = "es",
                Author = "José García",
                CreatedAt = DateTime.Now
            });
    }

    public override void Down()
    {
        Delete.Table("CaseInsensitiveData");
        Delete.Table("InternationalContent");
    }
}
```

### MySQL Security and User Management

```csharp
public class MySqlSecurity : Migration
{
    public override void Up()
    {
        // Create user management tables
        Create.Table("ApplicationUsers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("PasswordHash").AsString(255).NotNullable()
            .WithColumn("Salt").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("LastLoginAt").AsDateTime().Nullable()
            .WithColumn("FailedLoginAttempts").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LockedUntil").AsDateTime().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().NotNullable();

        Create.Table("UserSessions")
            .WithColumn("Id").AsString(128).NotNullable().PrimaryKey()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("IPAddress").AsString(45).NotNullable() // IPv6 compatible
            .WithColumn("UserAgent").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Create indexes for security tables
        Create.Index("UQ_ApplicationUsers_Username")
            .OnTable("ApplicationUsers")
            .OnColumn("Username")
            .Unique();

        Create.Index("UQ_ApplicationUsers_Email")
            .OnTable("ApplicationUsers")
            .OnColumn("Email")
            .Unique();

        Create.Index("IX_UserSessions_UserId")
            .OnTable("UserSessions")
            .OnColumn("UserId");

        Create.Index("IX_UserSessions_ExpiresAt")
            .OnTable("UserSessions")
            .OnColumn("ExpiresAt");

        // Foreign key relationship
        Create.ForeignKey("FK_UserSessions_ApplicationUsers")
            .FromTable("UserSessions").ForeignColumn("UserId")
            .ToTable("ApplicationUsers").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // Create stored procedure for secure login
        Execute.Sql(@"
            DELIMITER //
            CREATE PROCEDURE sp_ValidateUser(
                IN p_Username VARCHAR(50),
                IN p_PasswordHash VARCHAR(255),
                OUT p_UserId INT,
                OUT p_IsValid BOOLEAN,
                OUT p_IsLocked BOOLEAN
            )
            BEGIN
                DECLARE v_FailedAttempts INT DEFAULT 0;
                DECLARE v_LockedUntil DATETIME DEFAULT NULL;

                SELECT Id, FailedLoginAttempts, LockedUntil
                INTO p_UserId, v_FailedAttempts, v_LockedUntil
                FROM ApplicationUsers
                WHERE Username = p_Username
                AND PasswordHash = p_PasswordHash
                AND IsActive = 1;

                IF p_UserId IS NOT NULL THEN
                    SET p_IsValid = TRUE;
                    SET p_IsLocked = FALSE;

                    -- Reset failed attempts on successful login
                    UPDATE ApplicationUsers
                    SET FailedLoginAttempts = 0,
                        LockedUntil = NULL,
                        LastLoginAt = NOW()
                    WHERE Id = p_UserId;
                ELSE
                    SET p_IsValid = FALSE;
                    -- Check if account is locked
                    IF v_LockedUntil IS NOT NULL AND v_LockedUntil > NOW() THEN
                        SET p_IsLocked = TRUE;
                    ELSE
                        SET p_IsLocked = FALSE;
                    END IF;
                END IF;
            END//
            DELIMITER ;");
    }

    public override void Down()
    {
        Execute.Sql("DROP PROCEDURE IF EXISTS sp_ValidateUser");
        Delete.ForeignKey("FK_UserSessions_ApplicationUsers").OnTable("UserSessions");
        Delete.Table("UserSessions");
        Delete.Table("ApplicationUsers");
    }
}
```

## Common MySQL Migration Patterns

### MySQL Version-Specific Features

```csharp
public class MySqlVersionFeatures : Migration
{
    public override void Up()
    {
        // Check MySQL version and apply version-specific features
        var version = Execute.Sql("SELECT VERSION()").Returns<string>().FirstOrDefault();
        var majorVersion = ExtractMajorVersion(version);

        Create.Table("VersionSpecificFeatures")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();

        // MySQL 5.7+ features
        if (majorVersion >= 5.7)
        {
            // JSON data type
            Alter.Table("VersionSpecificFeatures")
                .AddColumn("JsonData").AsCustom("JSON").Nullable();

            // Generated columns
            Execute.Sql(@"
                ALTER TABLE VersionSpecificFeatures
                ADD COLUMN NameLength INT AS (CHAR_LENGTH(Name)) STORED");
        }

        // MySQL 8.0+ features
        if (majorVersion >= 8.0)
        {
            // Window functions and CTEs are available
            Execute.Sql(@"
                CREATE VIEW TopVersionFeatures AS
                WITH RankedFeatures AS (
                    SELECT
                        Name,
                        CreatedAt,
                        ROW_NUMBER() OVER (ORDER BY CreatedAt DESC) as RowNum
                    FROM VersionSpecificFeatures
                )
                SELECT Name, CreatedAt
                FROM RankedFeatures
                WHERE RowNum <= 10");

            // Invisible indexes (MySQL 8.0+)
            Execute.Sql(@"
                CREATE INDEX IX_VersionSpecificFeatures_Name_Invisible
                ON VersionSpecificFeatures (Name) INVISIBLE");
        }
    }

    private double ExtractMajorVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return 5.0;

        var parts = version.Split('.', '-');
        if (parts.Length >= 2 &&
            double.TryParse($"{parts[0]}.{parts[1]}", out double result))
        {
            return result;
        }

        return 5.0;
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS TopVersionFeatures");
        Delete.Table("VersionSpecificFeatures");
    }
}
```

## Troubleshooting MySQL Issues

### Common MySQL Migration Issues

```csharp
public class MySqlTroubleshooting : Migration
{
    public override void Up()
    {
        try
        {
            // Issue 1: Character set problems
            Create.Table("TestCharacterSet")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("TextData").AsCustom("TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci").NotNullable();

            // Issue 2: Foreign key constraint failures
            if (Schema.Table("Users").Exists())
            {
                Create.Table("UserProfiles")
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("UserId").AsInt32().NotNullable()
                    .WithColumn("ProfileData").AsCustom("JSON").Nullable();

                // Check for orphaned data before creating foreign key
                var orphanedCount = Execute.Sql(@"
                    SELECT COUNT(*)
                    FROM UserProfiles p
                    LEFT JOIN Users u ON p.UserId = u.Id
                    WHERE u.Id IS NULL").Returns<int>().FirstOrDefault();

                if (orphanedCount == 0)
                {
                    Create.ForeignKey("FK_UserProfiles_Users")
                        .FromTable("UserProfiles").ForeignColumn("UserId")
                        .ToTable("Users").PrimaryColumn("Id");
                }
            }

            // Issue 3: Index length limitations
            Create.Table("LongTextTable")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("LongText").AsCustom("TEXT").NotNullable();

            // Use prefix index for TEXT columns
            Execute.Sql("CREATE INDEX IX_LongTextTable_LongText ON LongTextTable (LongText(255))");

        }
        catch (Exception ex)
        {
            // Log the exception and provide helpful error information
            Execute.Sql($"-- Migration failed with error: {ex.Message}");
            throw;
        }
    }

    public override void Down()
    {
        Delete.Table("LongTextTable");

        if (Schema.Table("UserProfiles").Exists())
        {
            if (Schema.Table("UserProfiles").Constraint("FK_UserProfiles_Users").Exists())
            {
                Delete.ForeignKey("FK_UserProfiles_Users").OnTable("UserProfiles");
            }
            Delete.Table("UserProfiles");
        }

        Delete.Table("TestCharacterSet");
    }
}
```
