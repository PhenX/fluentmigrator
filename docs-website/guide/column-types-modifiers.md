# Column Types and Modifiers

FluentMigrator provides comprehensive support for database column types and modifiers, allowing you to define columns with appropriate data types and constraints across different database providers.

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
- Consider maximum identifier lengths for different databases