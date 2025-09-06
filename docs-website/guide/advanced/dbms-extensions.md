# DBMS Extensions

FluentMigrator provides database-specific extensions that allow you to use native features of each database provider while maintaining a mostly unified API.

## Overview

DBMS extensions enable you to:
- Access database-specific features not available in the core API
- Optimize for particular database providers
- Use advanced features like specialized indexes, data types, and constraints
- Maintain compatibility across different database providers when needed

## SQL Server Extensions

### Installation
```xml
<PackageReference Include="FluentMigrator.Extensions.SqlServer" Version="6.2.0" />
```

### Index Extensions

#### Included Columns (Covering Indexes)
```csharp
using FluentMigrator.SqlServer;

Create.Index("IX_Users_LastName_Covering").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .WithOptions().NonClustered()
    .Include("FirstName")
    .Include("Email")
    .Include("PhoneNumber");
```

#### Filtered Indexes
```csharp
Create.Index("IX_Orders_ActiveOnly").OnTable("Orders")
    .OnColumn("OrderDate").Descending()
    .WithOptions()
        .NonClustered()
        .Filter("[Status] = 'Active'");
```

#### Index Options
```csharp
Create.Index("IX_Products_Complex").OnTable("Products")
    .OnColumn("CategoryId").Ascending()
    .OnColumn("Name").Ascending()
    .WithOptions()
        .NonClustered()
        .WithFillFactor(80)
        .WithPadIndex()
        .WithIgnoreDuplicateKeys()
        .WithSortInTempDb()
        .WithDropExisting();
```

### Column Extensions

#### Identity with Custom Seed/Increment
```csharp
Create.Table("Invoices")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(1000, 5) // Start at 1000, increment by 5
    .WithColumn("InvoiceNumber").AsString(20).NotNullable();
```

#### Computed Columns
```csharp
Create.Table("OrderItems")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Quantity").AsInt32().NotNullable()
    .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable()
    .WithColumn("TotalPrice").AsDecimal(10, 2).Computed("[Quantity] * [UnitPrice]");

// Persisted computed column
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(255).NotNullable()
    .WithColumn("SearchName").AsString(255)
        .Computed("UPPER([Name])").Persisted();
```

#### Column Store Indexes (SQL Server 2012+)
```csharp
Create.Index("CIX_Sales_ColumnStore").OnTable("Sales")
    .WithOptions().Clustered().ColumnStore();

// Non-clustered column store with specific columns
Create.Index("NCIX_Sales_Partial").OnTable("Sales")
    .OnColumn("ProductId")
    .OnColumn("SaleDate")
    .OnColumn("Amount")
    .WithOptions().NonClustered().ColumnStore();
```

### Temporal Tables (SQL Server 2016+)
```csharp
[Migration(1)]
public class CreateTemporalTable : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.UsersHistory));
        ");
    }

    public override void Down()
    {
        Execute.Sql("ALTER TABLE Users SET (SYSTEM_VERSIONING = OFF)");
        Execute.Sql("DROP TABLE UsersHistory");
        Delete.Table("Users");
    }
}
```

## PostgreSQL Extensions

### Installation
```xml
<PackageReference Include="FluentMigrator.Extensions.Postgres" Version="6.2.0" />
```

### Index Extensions

#### Algorithm-Specific Indexes
```csharp
using FluentMigrator.Postgres;

// B-tree index (default)
Create.Index("IX_Users_Email_Btree").OnTable("Users")
    .OnColumn("Email")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.BTree);

// Hash index for equality comparisons
Create.Index("IX_Users_Status_Hash").OnTable("Users")
    .OnColumn("Status")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Hash);

// GIN index for full-text search and JSON
Create.Index("IX_Articles_Content_Gin").OnTable("Articles")
    .OnColumn("ContentVector")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);

// GiST index for geometric data and full-text search
Create.Index("IX_Locations_Point_Gist").OnTable("Locations")
    .OnColumn("Coordinates")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gist);
```

#### Partial Indexes
```csharp
Create.Index("IX_Users_ActiveEmail").OnTable("Users")
    .OnColumn("Email")
    .Where("is_active = true");

Create.Index("IX_Orders_RecentPending").OnTable("Orders")
    .OnColumn("CreatedAt").Descending()
    .Where("status = 'pending' AND created_at > NOW() - INTERVAL '30 days'");
```

#### Expression Indexes
```csharp
// Index on expression
Create.Index("IX_Users_LowerEmail").OnTable("Users")
    .OnColumn(RawSql.Insert("lower(email)"));

// Multi-column expression index
Create.Index("IX_Users_FullName").OnTable("Users")
    .OnColumn(RawSql.Insert("lower(first_name || ' ' || last_name)"));
```

### Column Extensions

#### Identity Columns (PostgreSQL 10+)
```csharp
using FluentMigrator.Postgres;

// GENERATED ALWAYS AS IDENTITY
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(PostgresGenerationType.Always, 1, 1)
    .WithColumn("Username").AsString(50).NotNullable();

// GENERATED BY DEFAULT AS IDENTITY
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(PostgresGenerationType.ByDefault, 100, 10)
    .WithColumn("Name").AsString(255).NotNullable();
```

#### Array Columns with Constraints
```csharp
Create.Table("UserRoles")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("UserId").AsInt32().NotNullable()
    .WithColumn("Roles").AsCustom("text[]").NotNullable()
    .WithColumn("Permissions").AsCustom("integer[]").Nullable();

// Check constraint on array
Create.CheckConstraint("CK_UserRoles_ValidRoles").OnTable("UserRoles")
    .Expression("array_length(roles, 1) > 0 AND array_length(roles, 1) <= 10");
```

### PostgreSQL-Specific Data Types
```csharp
Create.Table("AdvancedTypes")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("JsonData").AsCustom("JSONB").NotNullable()
    .WithColumn("UuidValue").AsGuid().NotNullable()
    .WithColumn("IpAddress").AsCustom("INET").Nullable()
    .WithColumn("MacAddress").AsCustom("MACADDR").Nullable()
    .WithColumn("IntegerRange").AsCustom("INT4RANGE").Nullable()
    .WithColumn("TimestampRange").AsCustom("TSRANGE").Nullable()
    .WithColumn("GeometricPoint").AsCustom("POINT").Nullable()
    .WithColumn("GeometricBox").AsCustom("BOX").Nullable()
    .WithColumn("NetworkCidr").AsCustom("CIDR").Nullable()
    .WithColumn("BitString").AsCustom("BIT(8)").Nullable()
    .WithColumn("VarBitString").AsCustom("VARBIT").Nullable();
```

### JSON/JSONB Operations
```csharp
// Table with JSONB column
Create.Table("UserPreferences")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("UserId").AsInt32().NotNullable()
    .WithColumn("Settings").AsCustom("JSONB").NotNullable();

// GIN index for JSONB operations
Create.Index("IX_UserPreferences_Settings_Gin").OnTable("UserPreferences")
    .OnColumn("Settings")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);

// Index on specific JSON key
Create.Index("IX_UserPreferences_Theme").OnTable("UserPreferences")
    .OnColumn(RawSql.Insert("(settings->>'theme')"));

// Check constraint on JSON
Create.CheckConstraint("CK_UserPreferences_ValidTheme").OnTable("UserPreferences")
    .Expression("settings ? 'theme' AND settings->>'theme' IN ('light', 'dark')");
```

## MySQL Extensions

### Installation
```xml
<PackageReference Include="FluentMigrator.Extensions.MySql" Version="6.2.0" />
```

### Storage Engine Specification
```csharp
using FluentMigrator.MySql;

[Migration(1)]
public class CreateMySqlTables : Migration
{
    public override void Up()
    {
        // InnoDB table (default for MySQL 5.5+)
        Execute.Sql(@"
CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Email VARCHAR(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        ");

        // MyISAM table (for read-heavy operations)
        Execute.Sql(@"
CREATE TABLE SearchCache (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Query VARCHAR(255) NOT NULL,
    Results TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4;
        ");
    }

    public override void Down()
    {
        Delete.Table("SearchCache");
        Delete.Table("Users");
    }
}
```

### Character Set and Collation
```csharp
Create.Table("Articles")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Title").AsString(255).NotNullable()
    .WithColumn("Content").AsString().NotNullable()
    .WithColumn("Slug").AsString(255).NotNullable();

// Set character set and collation
Execute.Sql("ALTER TABLE Articles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
```

### Full-Text Indexes
```csharp
Create.Table("Documents")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Title").AsString(255).NotNullable()
    .WithColumn("Content").AsString().NotNullable()
    .WithColumn("Tags").AsString(500).Nullable();

// Full-text index
Execute.Sql("CREATE FULLTEXT INDEX FTI_Documents_Content ON Documents (title, content)");

// Full-text index with parser
Execute.Sql("CREATE FULLTEXT INDEX FTI_Documents_Tags ON Documents (tags) WITH PARSER ngram");
```

## Oracle Extensions

### Installation
```xml
<PackageReference Include="FluentMigrator.Extensions.Oracle" Version="6.2.0" />
```

### Sequences and Triggers
```csharp
[Migration(1)]
public class CreateOracleSequences : Migration
{
    public override void Up()
    {
        // Create sequence
        Execute.Sql("CREATE SEQUENCE users_seq START WITH 1 INCREMENT BY 1 NOCACHE");

        // Create table
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable();

        // Create trigger for auto-increment
        Execute.Sql(@"
CREATE OR REPLACE TRIGGER users_trigger
BEFORE INSERT ON Users
FOR EACH ROW
BEGIN
    SELECT users_seq.NEXTVAL INTO :NEW.Id FROM dual;
END;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER users_trigger");
        Delete.Table("Users");
        Execute.Sql("DROP SEQUENCE users_seq");
    }
}
```

### Oracle Data Types
```csharp
Create.Table("OracleTypes")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
    .WithColumn("NumberValue").AsCustom("NUMBER(10,2)").NotNullable()
    .WithColumn("VarcharValue").AsCustom("VARCHAR2(255)").NotNullable()
    .WithColumn("ClobValue").AsCustom("CLOB").Nullable()
    .WithColumn("BlobValue").AsCustom("BLOB").Nullable()
    .WithColumn("DateValue").AsCustom("DATE").NotNullable()
    .WithColumn("TimestampValue").AsCustom("TIMESTAMP").NotNullable()
    .WithColumn("IntervalValue").AsCustom("INTERVAL DAY TO SECOND").Nullable();
```

## Snowflake Extensions

### Installation
```xml
<PackageReference Include="FluentMigrator.Extensions.Snowflake" Version="6.2.0" />
```

### Snowflake-Specific Features
```csharp
using FluentMigrator.Snowflake;

Create.Table("SnowflakeTable")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
    .WithColumn("Data").AsCustom("VARIANT").Nullable()    // Semi-structured data
    .WithColumn("ArrayData").AsCustom("ARRAY").Nullable()
    .WithColumn("ObjectData").AsCustom("OBJECT").Nullable()
    .WithColumn("GeographyData").AsCustom("GEOGRAPHY").Nullable();

// Cluster key for performance
Execute.Sql("ALTER TABLE SnowflakeTable CLUSTER BY (Id)");
```

## Cross-Provider Compatibility

### Conditional Extensions
```csharp
[Migration(1)]
public class CrossProviderMigration : Migration
{
    public override void Up()
    {
        // Common table structure
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // SQL Server specific features
        IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
            CREATE NONCLUSTERED INDEX IX_Users_Email_Covering 
            ON Users (Email) 
            INCLUDE (Username, CreatedAt)
        ");

        // PostgreSQL specific features
        IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
            CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
            CREATE INDEX CONCURRENTLY IX_Users_Email_Lower 
            ON Users (lower(Email));
        ");

        // MySQL specific features
        IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
            ALTER TABLE Users 
            ENGINE=InnoDB 
            DEFAULT CHARSET=utf8mb4 
            COLLATE=utf8mb4_unicode_ci;
        ");
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

### Provider Detection
```csharp
[Migration(1)]
public class ProviderSpecificMigration : Migration
{
    public override void Up()
    {
        var processorType = ApplicationContext.Connection.GetType().Name;

        if (processorType.Contains("SqlServer"))
        {
            // SQL Server specific code
            Execute.Sql("SELECT @@VERSION");
        }
        else if (processorType.Contains("Postgres"))
        {
            // PostgreSQL specific code
            Execute.Sql("SELECT version()");
        }
        else if (processorType.Contains("MySql"))
        {
            // MySQL specific code
            Execute.Sql("SELECT @@version");
        }
    }

    public override void Down()
    {
        // Rollback logic
    }
}
```

## Best Practices

### 1. Use Extensions Judiciously
```csharp
// Good - Use extensions for significant performance benefits
IfDatabase(ProcessorIdConstants.SqlServer)
    .Create.Index("IX_Users_Covering").OnTable("Users")
    .OnColumn("LastName")
    .Include("FirstName", "Email"); // Covering index for better performance

// Avoid - Using extensions for minor features that don't provide significant value
```

### 2. Maintain Compatibility When Possible
```csharp
// Good - Provide fallback for different providers
IfDatabase(ProcessorIdConstants.SqlServer)
    .Execute.Sql("CREATE UNIQUE INDEX IX_Users_Email ON Users (Email) WHERE IsActive = 1");

IfDatabase(ProcessorIdConstants.Postgres) 
    .Execute.Sql("CREATE UNIQUE INDEX IX_Users_Email ON Users (Email) WHERE IsActive = true");

IfDatabase(ProcessorIdConstants.MySql)
    .Create.UniqueConstraint("UQ_Users_Email").OnTable("Users").Column("Email");
```

### 3. Document Provider-Specific Features
```csharp
[Migration(1)]
public class ProviderSpecificFeatures : Migration
{
    /// <summary>
    /// Creates indexes optimized for each database provider:
    /// - SQL Server: Uses covering indexes for better query performance
    /// - PostgreSQL: Uses partial indexes for filtered queries
    /// - MySQL: Uses full-text indexes for search functionality
    /// </summary>
    public override void Up()
    {
        // Implementation...
    }

    public override void Down()
    {
        // Implementation...
    }
}
```

### 4. Test Across Providers
```csharp
// Use integration tests to verify migrations work across all target providers
[Test]
[TestCase("SqlServer")]
[TestCase("Postgres")]
[TestCase("MySql")]
public void Migration_ShouldWork_OnAllProviders(string provider)
{
    // Test migration on specific provider
}
```

## Common Patterns

### Feature Detection Pattern
```csharp
public static class DatabaseFeatures
{
    public static bool SupportsPartialIndexes(string processorId)
    {
        return processorId.Contains("Postgres") || processorId.Contains("SqlServer");
    }

    public static bool SupportsJsonDataType(string processorId)
    {
        return processorId.Contains("Postgres") || 
               processorId.Contains("MySql") ||
               (processorId.Contains("SqlServer") && GetSqlServerVersion() >= 2016);
    }
}
```

### Graceful Degradation Pattern
```csharp
[Migration(1)]
public class OptimizedIndexes : Migration
{
    public override void Up()
    {
        // Try to use advanced features, fall back to basic if not supported
        try
        {
            IfDatabase(ProcessorIdConstants.SqlServer)
                .Create.Index("IX_Users_Optimized").OnTable("Users")
                .OnColumn("LastName")
                .Include("FirstName", "Email");
        }
        catch
        {
            // Fallback to basic index
            Create.Index("IX_Users_Basic").OnTable("Users")
                .OnColumn("LastName");
        }
    }

    public override void Down()
    {
        // Cleanup logic
    }
}
```

## Next Steps

- [Edge Cases](./edge-cases.md) - Learn about handling edge cases in migrations
- [Best Practices](./best-practices.md) - Discover migration best practices
- [Provider Comparisons](../providers/others.md) - Compare features across providers