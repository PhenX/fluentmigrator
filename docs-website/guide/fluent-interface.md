# Fluent Interface Deep Dive

FluentMigrator's fluent API provides an intuitive, type-safe way to define database schema changes. This guide covers the complete fluent interface, from basic concepts to advanced usage patterns.

## Core Concepts

The FluentMigrator fluent API:
- **Builds a semantic model** behind the scenes for batch processing
- **Starts with five main root expressions**: Create, Alter, Delete, Execute, and Rename
- **Uses method chaining** for readable, self-documenting code
- **Provides database-agnostic** operations with database-specific extensions

## Root Expressions Overview

| Expression | Purpose | Common Operations |
|------------|---------|-------------------|
| **Create** | Add new database objects | Tables, columns, indexes, foreign keys, schemas |
| **Alter** | Modify existing objects | Add/modify columns, change table properties |
| **Delete** | Remove database objects | Drop tables, columns, indexes, constraints |
| **Execute** | Run raw SQL or scripts | Custom SQL, stored procedures, data operations |
| **Rename** | Rename database objects | Tables, columns |

## Create Expression

The Create expression allows you to add new database objects.

### Creating Tables

#### Basic Table Creation
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Username").AsString(50).NotNullable().Unique()
    .WithColumn("Email").AsString(255).NotNullable()
    .WithColumn("FirstName").AsString(100).Nullable()
    .WithColumn("LastName").AsString(100).Nullable()
    .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
    .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
```

#### Using Extension Methods
```csharp
Create.Table("Users")
    .WithIdColumn() // Extension method - creates Id column with Identity
    .WithColumn("Username").AsString(50).NotNullable().Unique()
    .WithColumn("Email").AsString(255).NotNullable()
    .WithTimeStamps(); // Extension method - adds CreatedAt/UpdatedAt
```

#### Table with Schema
```csharp
Create.Table("Products")
    .InSchema("Inventory")
    .WithIdColumn()
    .WithColumn("Name").AsString(200).NotNullable()
    .WithColumn("Price").AsDecimal(10, 2).NotNullable();
```

### Creating Indexes

#### Simple Index
```csharp
Create.Index("IX_Users_Email")
    .OnTable("Users")
    .OnColumn("Email");
```

#### Composite Index
```csharp
Create.Index("IX_Orders_CustomerDate")
    .OnTable("Orders")
    .OnColumn("CustomerId")
    .OnColumn("OrderDate");
```

#### Unique Index
```csharp
Create.Index("IX_Users_Username")
    .OnTable("Users")
    .OnColumn("Username")
    .Unique();
```

#### Index with Included Columns (SQL Server)
```csharp
Create.Index("IX_Orders_Customer_Covering")
    .OnTable("Orders")
    .OnColumn("CustomerId")
    .Include("OrderDate")
    .Include("TotalAmount");
```

### Creating Foreign Keys

#### Basic Foreign Key
```csharp
Create.ForeignKey("FK_Orders_Customer")
    .FromTable("Orders").ForeignColumn("CustomerId")
    .ToTable("Customers").PrimaryColumn("Id");
```

#### Foreign Key with Cascade Options
```csharp
Create.ForeignKey("FK_OrderItems_Order")
    .FromTable("OrderItems").ForeignColumn("OrderId")
    .ToTable("Orders").PrimaryColumn("Id")
    .OnDelete(Rule.Cascade)
    .OnUpdate(Rule.Restrict);
```

#### Composite Foreign Key
```csharp
Create.ForeignKey("FK_OrderItems_Product")
    .FromTable("OrderItems").ForeignColumns("ProductId", "VariantId")
    .ToTable("ProductVariants").PrimaryColumns("ProductId", "Id");
```

### Creating Schemas
```csharp
Create.Schema("Sales");
Create.Schema("Inventory");
Create.Schema("Reporting");
```

## Alter Expression

The Alter expression modifies existing database objects.

### Adding Columns
```csharp
Alter.Table("Users")
    .AddColumn("PhoneNumber").AsString(20).Nullable()
    .AddColumn("LastLoginAt").AsDateTime().Nullable()
    .AddColumn("LoginCount").AsInt32().NotNullable().WithDefaultValue(0);
```

### Modifying Columns

#### Change Column Type and Properties
```csharp
Alter.Table("Users")
    .AlterColumn("Email")
    .AsString(320) // Increase length
    .NotNullable();
```

#### Alternative Syntax
```csharp
Alter.Column("Email")
    .OnTable("Users")
    .AsString(320)
    .NotNullable();
```

### Adding Constraints
```csharp
// Add primary key
Alter.Table("Products")
    .AddPrimaryKey("PK_Products")
    .OnColumn("Id");

// Add unique constraint
Alter.Table("Users")
    .AddUniqueConstraint("UQ_Users_Email")
    .OnColumn("Email");

// Add check constraint
Alter.Table("Products")
    .AddCheckConstraint("CK_Products_Price")
    .Expression("Price > 0");
```

## Delete Expression

The Delete expression removes database objects.

### Dropping Tables
```csharp
Delete.Table("TempTable");
Delete.Table("OldProducts").InSchema("Archive");
```

### Dropping Columns

#### Single Column
```csharp
Delete.Column("ObsoleteField").FromTable("Users");
```

#### Multiple Columns
```csharp
Delete.Column("Field1")
      .Column("Field2")
      .Column("Field3")
      .FromTable("Users");
```

### Dropping Indexes
```csharp
Delete.Index("IX_Users_OldField").OnTable("Users");
```

### Dropping Foreign Keys
```csharp
Delete.ForeignKey("FK_Orders_OldCustomer").OnTable("Orders");
```

### Dropping Constraints
```csharp
Delete.PrimaryKey("PK_OldTable").FromTable("OldTable");
Delete.UniqueConstraint("UQ_Users_OldField").FromTable("Users");
```

## Execute Expression

The Execute expression runs custom SQL code.

### Raw SQL
```csharp
Execute.Sql("UPDATE Users SET IsActive = 1 WHERE CreatedAt > GETDATE() - 30");
```

### SQL Scripts
```csharp
// External script file
Execute.Script("InitialData.sql");

// Embedded resource script
Execute.EmbeddedScript("Scripts.CreateStoredProcedures.sql");
```

### Parameterized SQL
```csharp
Execute.Sql(@"
    INSERT INTO Settings (Key, Value, CreatedAt) 
    VALUES ('MaxUsers', '1000', GETUTCDATE())
");
```

## Rename Expression

The Rename expression changes object names.

### Renaming Tables
```csharp
Rename.Table("OldTableName").To("NewTableName");
Rename.Table("Products").InSchema("Old").To("LegacyProducts");
```

### Renaming Columns
```csharp
Rename.Column("OldColumnName")
      .OnTable("Users")
      .To("NewColumnName");
```

## Data Expressions

FluentMigrator provides specialized expressions for data manipulation.

### Inserting Data

#### Single Row
```csharp
Insert.IntoTable("Users").Row(new 
{ 
    Username = "admin", 
    Email = "admin@example.com",
    FirstName = "System",
    LastName = "Administrator",
    CreatedAt = DateTime.UtcNow
});
```

#### Multiple Rows
```csharp
Insert.IntoTable("Categories")
    .Row(new { Name = "Electronics", IsActive = true })
    .Row(new { Name = "Books", IsActive = true })
    .Row(new { Name = "Clothing", IsActive = true });
```

#### Non-Unicode Strings
```csharp
Insert.IntoTable("Products").Row(new 
{ 
    Name = new NonUnicodeString("Product Name"), // ANSI string
    Description = "Unicode description" // Unicode string
});
```

### Updating Data

#### Simple Update
```csharp
Update.Table("Users")
      .Set(new { IsActive = false })
      .Where(new { Username = "olduser" });
```

#### Update All Rows
```csharp
Update.Table("Products")
      .Set(new { UpdatedAt = DateTime.UtcNow })
      .AllRows();
```

#### Complex Update with SetExistingRowsTo
```csharp
Alter.Table("Users")
    .AddColumn("Status")
    .AsString(20)
    .SetExistingRowsTo("Active") // Set default for existing rows
    .NotNullable();
```

### Deleting Data

#### Delete Specific Rows
```csharp
Delete.FromTable("Users").Row(new { Username = "testuser" });
```

#### Delete with Null Condition
```csharp
Delete.FromTable("Users").IsNull("Email");
```

#### Delete All Rows
```csharp
Delete.FromTable("TempData").AllRows();
```

## Conditional Logic

### Database-Specific Operations

#### IfDatabase Expression
```csharp
IfDatabase("SqlServer", "Postgres")
    .Create.Table("Users")
    .WithIdColumn()
    .WithColumn("Name").AsString().NotNullable();

IfDatabase("Sqlite")
    .Create.Table("Users")
    .WithColumn("Id").AsInt16().PrimaryKey() // SQLite uses different approach
    .WithColumn("Name").AsString().NotNullable();
```

#### Database Provider Support
```csharp
IfDatabase("SqlServer")
    .Execute.Sql("CREATE FULLTEXT CATALOG ProductCatalog"); // SQL Server specific

IfDatabase("Postgres") 
    .Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\""); // PostgreSQL specific

IfDatabase("MySql")
    .Execute.Sql("SET sql_mode = 'STRICT_TRANS_TABLES'"); // MySQL specific
```

### Schema Existence Checks

#### Check Table Existence
```csharp
if (!Schema.Table("Users").Exists())
{
    Create.Table("Users")
        .WithIdColumn()
        .WithColumn("Name").AsString(100).NotNullable();
}
```

#### Check Column Existence
```csharp
if (!Schema.Table("Users").Column("Email").Exists())
{
    Alter.Table("Users")
        .AddColumn("Email").AsString(255).Nullable();
}
```

#### Check Index Existence
```csharp
if (!Schema.Table("Users").Index("IX_Users_Email").Exists())
{
    Create.Index("IX_Users_Email")
        .OnTable("Users")
        .OnColumn("Email");
}
```

#### Check Constraint Existence
```csharp
if (!Schema.Table("Users").Constraint("UQ_Users_Username").Exists())
{
    Alter.Table("Users")
        .AddUniqueConstraint("UQ_Users_Username")
        .OnColumn("Username");
}
```

## Column Data Types

### Basic Types
```csharp
.WithColumn("StringCol").AsString(100)           // VARCHAR(100)
.WithColumn("AnsiStringCol").AsAnsiString(50)    // VARCHAR(50) non-Unicode
.WithColumn("TextCol").AsString()                // TEXT/VARCHAR(MAX)
.WithColumn("IntCol").AsInt32()                  // INT
.WithColumn("LongCol").AsInt64()                 // BIGINT
.WithColumn("ShortCol").AsInt16()                // SMALLINT
.WithColumn("ByteCol").AsByte()                  // TINYINT
.WithColumn("DecimalCol").AsDecimal(10, 2)       // DECIMAL(10,2)
.WithColumn("MoneyCol").AsCurrency()             // MONEY
.WithColumn("FloatCol").AsFloat()                // FLOAT
.WithColumn("DoubleCol").AsDouble()              // DOUBLE
.WithColumn("DateCol").AsDate()                  // DATE
.WithColumn("DateTimeCol").AsDateTime()          // DATETIME
.WithColumn("DateTime2Col").AsDateTime2()        // DATETIME2
.WithColumn("TimeCol").AsTime()                  // TIME
.WithColumn("BoolCol").AsBoolean()               // BIT/BOOLEAN
.WithColumn("GuidCol").AsGuid()                  // UNIQUEIDENTIFIER/UUID
.WithColumn("BinaryCol").AsBinary(100)           // VARBINARY(100)
.WithColumn("BlobCol").AsBinary()                // BLOB/VARBINARY(MAX)
```

### Database-Specific Types
```csharp
// SQL Server specific
.WithColumn("XmlCol").AsXml()                    // XML
.WithColumn("JsonCol").AsCustom("NVARCHAR(MAX)") // Custom type

// PostgreSQL specific  
.WithColumn("JsonCol").AsCustom("jsonb")         // JSONB
.WithColumn("ArrayCol").AsCustom("text[]")       // Array type

// MySQL specific
.WithColumn("JsonCol").AsCustom("JSON")          // JSON
.WithColumn("EnumCol").AsCustom("ENUM('A','B','C')") // ENUM
```

## Column Modifiers

### Nullability and Defaults
```csharp
.WithColumn("Name")
    .AsString(100)
    .NotNullable()                               // NOT NULL
    .WithDefaultValue("Unknown")                 // DEFAULT 'Unknown'

.WithColumn("CreatedAt")
    .AsDateTime()
    .NotNullable()
    .WithDefaultValue(SystemMethods.CurrentDateTime) // DEFAULT GETDATE()

.WithColumn("Count")
    .AsInt32()
    .Nullable()                                  // NULL (default)
    .WithDefaultValue(0)                        // DEFAULT 0
```

### Identity and Primary Keys
```csharp
.WithColumn("Id")
    .AsInt32()
    .NotNullable()
    .PrimaryKey()                               // PRIMARY KEY
    .Identity()                                 // IDENTITY/AUTO_INCREMENT

.WithColumn("Code")
    .AsString(10)
    .NotNullable()
    .PrimaryKey()                              // String primary key
    .Unique()                                  // UNIQUE constraint
```

### Indexing
```csharp
.WithColumn("Email")
    .AsString(255)
    .NotNullable()
    .Indexed()                                 // Creates index automatically
    .Unique()                                  // UNIQUE constraint
```

## Advanced Patterns

### Idempotent Migrations
```csharp
public override void Up()
{
    // Only create if doesn't exist
    if (!Schema.Table("Users").Exists())
    {
        Create.Table("Users")
            .WithIdColumn()
            .WithColumn("Name").AsString(100).NotNullable();
    }
    
    // Only add column if doesn't exist
    if (!Schema.Table("Users").Column("Email").Exists())
    {
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).Nullable();
    }
}
```

### Multi-Step Column Changes
```csharp
public override void Up()
{
    // Step 1: Add new nullable column
    Alter.Table("Users")
        .AddColumn("NewEmail")
        .AsString(320)
        .Nullable();

    // Step 2: Copy data from old column
    Execute.Sql("UPDATE Users SET NewEmail = Email");

    // Step 3: Make new column NOT NULL
    Alter.Column("NewEmail")
        .OnTable("Users")
        .AsString(320)
        .NotNullable();

    // Step 4: Drop old column
    Delete.Column("Email").FromTable("Users");

    // Step 5: Rename new column
    Rename.Column("NewEmail")
        .OnTable("Users")
        .To("Email");
}
```

### Complex Data Seeding
```csharp
public override void Up()
{
    // Create lookup tables first
    Create.Table("Roles")
        .WithIdColumn()
        .WithColumn("Name").AsString(50).NotNullable().Unique();

    // Seed roles
    Insert.IntoTable("Roles")
        .Row(new { Id = 1, Name = "Admin" })
        .Row(new { Id = 2, Name = "User" })
        .Row(new { Id = 3, Name = "Guest" });

    // Create main table with foreign key
    Create.Table("Users")
        .WithIdColumn()
        .WithColumn("Username").AsString(50).NotNullable()
        .WithColumn("RoleId").AsInt32().NotNullable();

    Create.ForeignKey("FK_Users_Role")
        .FromTable("Users").ForeignColumn("RoleId")
        .ToTable("Roles").PrimaryColumn("Id");

    // Seed users with roles
    Insert.IntoTable("Users")
        .Row(new { Username = "admin", RoleId = 1 })
        .Row(new { Username = "user", RoleId = 2 });
}
```

## Extension Methods

You can create custom extension methods to encapsulate common patterns:

```csharp
public static class MigrationExtensions
{
    public static ICreateTableColumnOptionOrWithColumnSyntax WithIdColumn(
        this ICreateTableWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("Id")
            .AsInt32()
            .NotNullable()
            .PrimaryKey()
            .Identity();
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithTimeStamps(
        this ICreateTableColumnOptionOrWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("CreatedAt")
                .AsDateTime()
                .NotNullable()
                .WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt")
                .AsDateTime()
                .Nullable();
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithAuditFields(
        this ICreateTableColumnOptionOrWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithTimeStamps()
            .WithColumn("CreatedBy")
                .AsString(100)
                .Nullable()
            .WithColumn("UpdatedBy")
                .AsString(100)
                .Nullable();
    }
}
```

**Usage:**
```csharp
Create.Table("Products")
    .WithIdColumn()           // Extension method
    .WithColumn("Name").AsString(200).NotNullable()
    .WithColumn("Price").AsDecimal(10, 2).NotNullable()
    .WithAuditFields();       // Extension method
```

## Best Practices

### ✅ Do
- Use meaningful names for tables, columns, and constraints
- Check for existence before creating/dropping objects
- Use appropriate data types and lengths
- Include proper indexes for foreign keys
- Use extension methods for common patterns
- Handle both Up() and Down() methods
- Use database-specific features when necessary

### ❌ Don't
- Hardcode database-specific syntax unless using IfDatabase()
- Forget to handle existing data when changing column types
- Create indexes without considering performance impact
- Use overly long constraint names (database limits apply)
- Mix data operations with schema changes unnecessarily

The fluent interface provides a powerful, readable way to define your database schema changes while maintaining database portability where possible.