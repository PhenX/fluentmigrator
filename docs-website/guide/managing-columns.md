# Managing Columns

This comprehensive guide covers all aspects of column management in FluentMigrator, including data types, modifiers, constraints, and best practices for working with database columns across different providers.

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

## Best Practices

### Data Type Selection
- Use appropriate precision for `DECIMAL` types: `AsDecimal(10, 2)` for currency
- Consider `VARCHAR` length limits across different database providers
- Use `AsAnsiString()` for non-Unicode strings when appropriate
- Choose `AsInt32()` vs `AsInt64()` based on expected data range

### Nullability Guidelines
- Make columns `NotNullable()` when business logic requires values
- Provide appropriate `WithDefaultValue()` for NOT NULL columns
- Use `SystemMethods.CurrentDateTime` for timestamp defaults
- Consider database-specific defaults with `IfDatabase()` conditionals

### Performance Considerations
- Use `Indexed()` for frequently queried columns
- Avoid over-indexing - each index has maintenance overhead
- Consider composite indexes for multi-column queries
- Use appropriate string lengths to optimize storage and performance

### Cross-Database Compatibility
- Test data types across all target database providers
- Use `AsCustom()` with `IfDatabase()` for provider-specific types
- Be aware of different NULL handling across providers

## Column Operations

### Adding Columns to Existing Tables
```csharp
public class AddColumns : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).NotNullable()
            .AddColumn("PhoneNumber").AsString(20).Nullable()
            .AddColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Column("Email").FromTable("Users");
        Delete.Column("PhoneNumber").FromTable("Users");
        Delete.Column("CreatedAt").FromTable("Users");
    }
}
```

### Modifying Existing Columns
```csharp
public class ModifyColumns : Migration
{
    public override void Up()
    {
        // Change data type
        Alter.Column("Price").OnTable("Products")
            .AsDecimal(10, 2).NotNullable();
            
        // Change nullability
        Alter.Column("Description").OnTable("Products")
            .AsString(500).Nullable();
            
        // Add default value
        Alter.Column("IsActive").OnTable("Users")
            .AsBoolean().NotNullable().WithDefaultValue(true);
    }

    public override void Down()
    {
        Alter.Column("Price").OnTable("Products")
            .AsString(50).Nullable();
        Alter.Column("Description").OnTable("Products")
            .AsString(500).NotNullable();
        Alter.Column("IsActive").OnTable("Users")
            .AsBoolean().Nullable();
    }
}
```

### Removing Columns
```csharp
public class RemoveColumns : Migration
{
    public override void Up()
    {
        Delete.Column("ObsoleteField").FromTable("Users");
    }

    public override void Down()
    {
        Alter.Table("Users")
            .AddColumn("ObsoleteField").AsString(100).Nullable();
    }
}
```

## Best Practices

### Choose Appropriate Data Types
- Use specific sizes for strings to prevent over-allocation
- Use appropriate numeric types (AsInt16() for small values, AsInt64() for large numbers)  
- Use decimal for monetary values, not float/double
- Use DateTime2 instead of DateTime in SQL Server for better precision

### Handle Column Modifications Safely
When changing column data types or sizes, always:
1. Check existing data compatibility first
2. Consider data migration requirements
3. Test the changes in a non-production environment
4. Have a rollback plan ready

### Performance Considerations
- Choose optimal data types for storage efficiency
- Use appropriate string lengths
- Consider indexing strategy for frequently queried columns
- See [Managing Indexes](/guide/managing-indexes) for detailed indexing guidance

## See Also
- [Creating Tables](/guide/operations/create-tables) - Complete table creation guide
- [Managing Indexes](/guide/managing-indexes) - Index optimization and management
- [Working with Constraints](/guide/working-with-constraints) - Constraint management
- [Working with Foreign Keys](/guide/working-with-foreign-keys) - Relationship management
- Consider maximum identifier lengths for different databases