# PostgreSQL Provider

FluentMigrator provides comprehensive support for PostgreSQL with extensions for PostgreSQL-specific features.

## Supported Versions

FluentMigrator supports:
- **PostgreSQL 16** ✅ (Latest)
- **PostgreSQL 15** ✅ 
- **PostgreSQL 14** ✅
- **PostgreSQL 13** ✅
- **PostgreSQL 12** ✅
- **PostgreSQL 11** ✅ (Minimum supported)

## Installation

Install the PostgreSQL provider package:

```xml
<PackageReference Include="FluentMigrator.Runner.Postgres" Version="6.2.0" />
<PackageReference Include="FluentMigrator.Extensions.Postgres" Version="6.2.0" />
```

## Configuration

### Basic Configuration
```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString("Host=localhost;Database=myapp;Username=myuser;Password=mypass")
        .ScanIn(typeof(MyMigration).Assembly).For.Migrations());
```

### Connection String Examples

#### Basic Connection
```csharp
"Host=localhost;Database=myapp;Username=myuser;Password=mypass"
```

#### With Port and SSL
```csharp
"Host=localhost;Port=5432;Database=myapp;Username=myuser;Password=mypass;SSL Mode=Require"
```

#### Connection Pooling
```csharp
"Host=localhost;Database=myapp;Username=myuser;Password=mypass;Pooling=true;MinPoolSize=1;MaxPoolSize=20"
```

#### AWS RDS PostgreSQL
```csharp
"Host=myinstance.region.rds.amazonaws.com;Port=5432;Database=myapp;Username=myuser;Password=mypass;SSL Mode=Require"
```

## PostgreSQL Specific Features

### Data Types

#### PostgreSQL Native Types
```csharp
using FluentMigrator.Postgres;

Create.Table("PostgresTypes")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("JsonData").AsCustom("JSONB").Nullable()
    .WithColumn("JsonTextData").AsCustom("JSON").Nullable()
    .WithColumn("UuidValue").AsGuid().NotNullable()
    .WithColumn("InetAddress").AsCustom("INET").Nullable()
    .WithColumn("MacAddress").AsCustom("MACADDR").Nullable()
    .WithColumn("IntRange").AsCustom("INT4RANGE").Nullable()
    .WithColumn("TimestampRange").AsCustom("TSRANGE").Nullable()
    .WithColumn("PointGeometry").AsCustom("POINT").Nullable()
    .WithColumn("ArrayInts").AsCustom("INTEGER[]").Nullable()
    .WithColumn("ArrayStrings").AsCustom("TEXT[]").Nullable();
```

#### Serial Types (Auto-increment)
```csharp
Create.Table("SerialTypes")
    .WithColumn("Id").AsCustom("SERIAL").NotNullable().PrimaryKey()      // 4-byte auto-incrementing integer
    .WithColumn("BigId").AsCustom("BIGSERIAL").NotNullable()             // 8-byte auto-incrementing integer
    .WithColumn("SmallId").AsCustom("SMALLSERIAL").NotNullable();        // 2-byte auto-incrementing integer
```

### Sequences

#### Creating Sequences
```csharp
Create.Sequence("user_id_seq")
    .StartWith(1000)
    .IncrementBy(1)
    .MinValue(1000)
    .MaxValue(999999999)
    .Cache(50);
```

#### Using Sequences
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .WithDefaultValue(RawSql.Insert("nextval('user_id_seq')"))
    .WithColumn("Username").AsString(50).NotNullable();
```

### Indexes

#### PostgreSQL Index Types
```csharp
using FluentMigrator.Postgres;

Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
    .WithColumn("Email").AsString(255).NotNullable()
    .WithColumn("JsonData").AsCustom("JSONB").Nullable()
    .WithColumn("SearchVector").AsCustom("TSVECTOR").Nullable();

// B-tree index (default)
Create.Index("IX_Users_Email").OnTable("Users")
    .OnColumn("Email").Ascending();

// Hash index
Create.Index("IX_Users_Email_Hash").OnTable("Users")
    .OnColumn("Email")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Hash);

// GIN index for JSONB
Create.Index("IX_Users_JsonData").OnTable("Users")
    .OnColumn("JsonData")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);

// GiST index for full-text search
Create.Index("IX_Users_SearchVector").OnTable("Users")
    .OnColumn("SearchVector")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gist);
```

#### Partial Indexes
```csharp
Create.Index("IX_Users_Active").OnTable("Users")
    .OnColumn("Email")
    .Where("is_active = true");
```

#### Expression Indexes
```csharp
Create.Index("IX_Users_LowerEmail").OnTable("Users")
    .OnColumn(RawSql.Insert("lower(email)"));
```

### Extensions

#### Enable Extensions
```csharp
[Migration(1)]
public class EnableExtensions : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\"");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"btree_gin\"");
    }

    public override void Down()
    {
        Execute.Sql("DROP EXTENSION IF EXISTS \"btree_gin\"");
        Execute.Sql("DROP EXTENSION IF EXISTS \"pg_trgm\"");
        Execute.Sql("DROP EXTENSION IF EXISTS \"uuid-ossp\"");
    }
}
```

#### Using UUID Extension
```csharp
Create.Table("Documents")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
        .WithDefaultValue(RawSql.Insert("uuid_generate_v4()"))
    .WithColumn("Title").AsString(255).NotNullable();
```

### PostgreSQL Extensions Usage

#### Identity Columns (PostgreSQL 10+)
```csharp
using FluentMigrator.Postgres;

Create.Table("ModernUsers")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(PostgresGenerationType.Always, 1, 1) // GENERATED ALWAYS AS IDENTITY
    .WithColumn("Username").AsString(50).NotNullable();

Create.Table("FlexibleUsers")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(PostgresGenerationType.ByDefault, 100, 10) // GENERATED BY DEFAULT AS IDENTITY (START 100 INCREMENT 10)
    .WithColumn("Username").AsString(50).NotNullable();
```

#### JSON Operations
```csharp
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Data").AsCustom("JSONB").NotNullable()
    .WithColumn("Metadata").AsCustom("JSON").Nullable();

// Index on JSON property
Create.Index("IX_Products_JsonCategory").OnTable("Products")
    .OnColumn(RawSql.Insert("(data->>'category')"));

// GIN index for JSON containment queries
Create.Index("IX_Products_DataGin").OnTable("Products")
    .OnColumn("Data")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);
```

### Full-Text Search

#### Text Search Configuration
```csharp
Create.Table("Articles")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Title").AsString(255).NotNullable()
    .WithColumn("Content").AsString().NotNullable()
    .WithColumn("SearchVector").AsCustom("TSVECTOR").Nullable();

// Create a trigger to automatically update search vector
Execute.Sql(@"
CREATE OR REPLACE FUNCTION update_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := to_tsvector('english', coalesce(NEW.title,'') || ' ' || coalesce(NEW.content,''));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_articles_search_vector
    BEFORE INSERT OR UPDATE ON Articles
    FOR EACH ROW EXECUTE FUNCTION update_search_vector();
");

// GiST index for text search
Create.Index("IX_Articles_SearchVector").OnTable("Articles")
    .OnColumn("SearchVector")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gist);
```

### Arrays

#### Array Columns
```csharp
Create.Table("UserPermissions")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("UserId").AsInt32().NotNullable()
    .WithColumn("Permissions").AsCustom("TEXT[]").NotNullable()
    .WithColumn("Tags").AsCustom("INTEGER[]").Nullable();

// GIN index for array operations
Create.Index("IX_UserPermissions_Permissions").OnTable("UserPermissions")
    .OnColumn("Permissions")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);
```

### Constraints

#### Check Constraints with PostgreSQL Features
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Email").AsString(255).NotNullable()
    .WithColumn("Age").AsInt32().NotNullable()
    .WithColumn("Settings").AsCustom("JSONB").NotNullable();

Create.CheckConstraint("CK_Users_Email_Valid")
    .OnTable("Users")
    .Expression("email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'"); // Regex validation

Create.CheckConstraint("CK_Users_Age_Valid")
    .OnTable("Users")
    .Expression("age >= 0 AND age <= 150");

Create.CheckConstraint("CK_Users_Settings_HasTheme")
    .OnTable("Users")
    .Expression("settings ? 'theme'"); // JSON key exists
```

### Enums

#### PostgreSQL Enums
```csharp
[Migration(1)]
public class CreateEnumTypes : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE TYPE user_status AS ENUM ('active', 'inactive', 'pending', 'banned')");
        Execute.Sql("CREATE TYPE priority_level AS ENUM ('low', 'medium', 'high', 'urgent')");

        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Status").AsCustom("user_status").NotNullable().WithDefaultValue(RawSql.Insert("'pending'"));

        Create.Table("Tasks")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(255).NotNullable()
            .WithColumn("Priority").AsCustom("priority_level").NotNullable().WithDefaultValue(RawSql.Insert("'medium'"));
    }

    public override void Down()
    {
        Delete.Table("Tasks");
        Delete.Table("Users");
        Execute.Sql("DROP TYPE IF EXISTS priority_level");
        Execute.Sql("DROP TYPE IF EXISTS user_status");
    }
}
```

### Table Inheritance

#### PostgreSQL Table Inheritance
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(100).NotNullable()
    .WithColumn("Email").AsString(255).NotNullable();

// Child table inheriting from Users
Execute.Sql(@"
CREATE TABLE Employees (
    employee_id SERIAL,
    department_id INTEGER NOT NULL
) INHERITS (Users)");
```

## Schemas and Databases

### Multiple Schemas
```csharp
Create.Schema("sales");
Create.Schema("inventory");

Create.Table("Products").InSchema("inventory")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(255).NotNullable();

Create.Table("Orders").InSchema("sales")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("ProductId").AsInt32().NotNullable()
        .ForeignKey("FK_Orders_Products", "inventory", "Products", "Id");
```

### Search Path Configuration
```csharp
Execute.Sql("SET search_path TO sales, inventory, public");
```

## Performance Optimization

### VACUUM and ANALYZE
```csharp
[Migration(1)]
public class MaintenanceMigration : Migration
{
    public override void Up()
    {
        // Perform maintenance after large data operations
        Execute.Sql("VACUUM ANALYZE Users");
        Execute.Sql("REINDEX INDEX IX_Users_Email");
    }

    public override void Down()
    {
        // Nothing to do
    }
}
```

### Connection Pooling
```csharp
// Connection string with pooling
"Host=localhost;Database=myapp;Username=myuser;Password=mypass;Pooling=true;MinPoolSize=5;MaxPoolSize=100;Connection Lifetime=300"
```

## Best Practices

### 1. Use Appropriate PostgreSQL Types
```csharp
// Use JSONB for queryable JSON data
.WithColumn("Settings").AsCustom("JSONB").NotNullable()

// Use JSON for write-heavy JSON data
.WithColumn("LogData").AsCustom("JSON").NotNullable()

// Use arrays for list data
.WithColumn("Tags").AsCustom("TEXT[]").Nullable()
```

### 2. Create Proper Indexes
```csharp
// GIN indexes for JSONB
Create.Index("IX_Settings_Gin").OnTable("Users")
    .OnColumn("Settings")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);

// Partial indexes for filtered queries
Create.Index("IX_Users_Active").OnTable("Users")
    .OnColumn("Email")
    .Where("is_active = true");
```

### 3. Use Extensions Wisely
```csharp
// Enable commonly used extensions
Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");   // UUID functions
Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\"");     // Trigram matching
Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"unaccent\"");    // Remove accents
```

### 4. Handle Text Search Properly
```csharp
// Use tsvector for full-text search
.WithColumn("SearchVector").AsCustom("TSVECTOR").Nullable()

// Create appropriate indexes
Create.Index("IX_SearchVector").OnTable("Articles")
    .OnColumn("SearchVector")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);
```

## Common Issues and Solutions

### Issue: Case Sensitivity
PostgreSQL is case-sensitive for identifiers. Use consistent naming.

```csharp
// Good - consistent lowercase
Create.Table("users")
    .WithColumn("user_id").AsInt32().NotNullable().PrimaryKey()
    .WithColumn("user_name").AsString(100).NotNullable();

// Or use quoted identifiers for mixed case
Create.Table("Users")
    .WithColumn("UserId").AsInt32().NotNullable().PrimaryKey()
    .WithColumn("UserName").AsString(100).NotNullable();
```

### Issue: UUID Generation
Enable the uuid-ossp extension before using UUID functions:

```csharp
Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");

Create.Table("Documents")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
        .WithDefaultValue(RawSql.Insert("uuid_generate_v4()"));
```

### Issue: JSON Operations
Use JSONB for better performance with JSON operations:

```csharp
// Better performance for queries
.WithColumn("Data").AsCustom("JSONB").NotNullable()

// Create GIN index for JSON operations
Create.Index("IX_Data_Gin").OnTable("MyTable")
    .OnColumn("Data")
    .UsingIndexAlgorithm(PostgresIndexAlgorithm.Gin);
```

## Migration Patterns

### Conditional PostgreSQL Features
```csharp
IfDatabase("Postgres")
    .Create.Table("PostgresOnlyTable")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("JsonData").AsCustom("JSONB").NotNullable();
```

### Version-Specific Features
```csharp
[Migration(1)]
public class PostgresVersionSpecific : Migration
{
    public override void Up()
    {
        // Check PostgreSQL version and use appropriate features
        var version = ApplicationContext.Connection.ServerVersion;
        
        if (version.StartsWith("10") || version.StartsWith("11") || 
            version.StartsWith("12") || version.StartsWith("13") ||
            version.StartsWith("14") || version.StartsWith("15") || 
            version.StartsWith("16"))
        {
            // Use GENERATED ALWAYS AS IDENTITY (PostgreSQL 10+)
            Execute.Sql(@"
                CREATE TABLE modern_users (
                    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    username VARCHAR(50) NOT NULL
                )");
        }
        else
        {
            // Use SERIAL for older versions
            Create.Table("modern_users")
                .WithColumn("id").AsCustom("SERIAL").NotNullable().PrimaryKey()
                .WithColumn("username").AsString(50).NotNullable();
        }
    }

    public override void Down()
    {
        Delete.Table("modern_users");
    }
}
```

## Next Steps

- [MySQL Provider](./mysql.md) - Learn about MySQL-specific features
- [SQLite Provider](./sqlite.md) - Understand SQLite considerations
- [DBMS Extensions](../advanced/dbms-extensions.md) - Explore cross-provider extensions