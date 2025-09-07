# Columns

This comprehensive guide covers all aspects of column management in FluentMigrator, including data types, modifiers, constraints, and best practices for working with database columns across different providers.

github.com/fluentmigrator/fluentmigrator/blob/main/src/FluentMigrator.Runner.Postgres/Generators/Postgres/PostgresTypeMap.cs

## Column Data Types

### Basic Types
```csharp
.WithColumn("StringCol").AsString(100)
.WithColumn("AnsiStringCol").AsAnsiString(50)
.WithColumn("TextCol").AsString()
.WithColumn("IntCol").AsInt32()
.WithColumn("LongCol").AsInt64()
.WithColumn("ShortCol").AsInt16()
.WithColumn("ByteCol").AsByte()
.WithColumn("DecimalCol").AsDecimal(10, 2)
.WithColumn("MoneyCol").AsCurrency()
.WithColumn("FloatCol").AsFloat()
.WithColumn("DoubleCol").AsDouble()
.WithColumn("DateCol").AsDate()
.WithColumn("DateTimeCol").AsDateTime()
.WithColumn("DateTime2Col").AsDateTime2()
.WithColumn("TimeCol").AsTime()
.WithColumn("BoolCol").AsBoolean()
.WithColumn("GuidCol").AsGuid()
.WithColumn("BinaryCol").AsBinary(100)
.WithColumn("BlobCol").AsBinary()
.WithColumn("XmlCol").AsXml()
.WithColumn("JsonCol").AsCustom("ENUM('A','B','C')") // Custom type, depending on DB
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
### Computed Columns

#### Basic Computed Column
```csharp
Create.Table("Orders")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Subtotal").AsDecimal(10, 2).NotNullable()
    .WithColumn("Tax").AsDecimal(10, 2).NotNullable()
    .WithColumn("Total").AsDecimal(10, 2).Computed("Subtotal + Tax");
```

#### Persisted Computed Column
```csharp
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(255).NotNullable()
    .WithColumn("SearchName").AsString(255).Computed("UPPER(Name)").Persisted();
```

### Setting Initial Values for Existing Rows

When adding new columns to existing tables with data, you may need to populate the new column with initial values for existing rows. The `SetExistingRowsTo` method provides this functionality.

#### Basic Usage
```csharp
public class AddLastLoginDate : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("LastLoginDate")
            .AsDateTime()
            .NotNullable()
            .SetExistingRowsTo(DateTime.Today);
    }

    public override void Down()
    {
        Delete.Column("LastLoginDate").FromTable("Users");
    }
}
```

#### With Nullable Columns
```csharp
public class AddOptionalField : Migration
{
    public override void Up()
    {
        Alter.Table("Products")
            .AddColumn("Category")
            .AsString(50)
            .Nullable()
            .SetExistingRowsTo("Uncategorized");
    }

    public override void Down()
    {
        Delete.Column("Category").FromTable("Products");
    }
}
```

#### Database-Specific Handling
```csharp
public class AddTimestampField : Migration
{
    public override void Up()
    {
        // SQLite doesn't support adding NOT NULL columns to existing tables
        // so we handle it differently
        IfDatabase(t => t != ProcessorIdConstants.SQLite)
            .Alter.Table("Orders")
            .AddColumn("CreatedAt")
            .AsDateTime()
            .NotNullable()
            .SetExistingRowsTo(SystemMethods.CurrentDateTime);

        IfDatabase(ProcessorIdConstants.SQLite)
            .Alter.Table("Orders")
            .AddColumn("CreatedAt")
            .AsDateTime()
            .Nullable()
            .SetExistingRowsTo(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Column("CreatedAt").FromTable("Orders");
    }
}
```

#### Complex Value Assignment
```csharp
public class AddCalculatedField : Migration
{
    public override void Up()
    {
        Alter.Table("Employees")
            .AddColumn("FullName")
            .AsString(255)
            .NotNullable()
            .SetExistingRowsTo("Unknown");

        // Update with calculated values using SQL
        Execute.Sql(@"
            UPDATE Employees 
            SET FullName = CONCAT(FirstName, ' ', LastName) 
            WHERE FirstName IS NOT NULL AND LastName IS NOT NULL
        ");
    }

    public override void Down()
    {
        Delete.Column("FullName").FromTable("Employees");
    }
}
```

#### Important Notes

- `SetExistingRowsTo` only works when **creating** new columns, not when altering existing ones
- The method automatically handles the sequence of operations: it first adds the column as nullable, updates existing rows with the specified value, then makes the column NOT NULL if specified
- For NOT NULL columns, FluentMigrator automatically creates the necessary UPDATE statement to populate existing rows before applying the NOT NULL constraint
- The value provided must be compatible with the column's data type
- Consider using database-specific logic when dealing with different database providers that have varying limitations

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

### Data Type Selection
- Use appropriate precision for `DECIMAL` types: `AsDecimal(10, 2)` for currency
- Consider `VARCHAR` length limits across different database providers
- Use `AsAnsiString()` for non-Unicode strings when appropriate
- Choose `AsInt32()` vs `AsInt64()` based on expected data range

### Nullability Guidelines
- Make columns `NotNullable()` when business logic requires values
- Specify explicitly `Nullable()` for optional fields
- Provide appropriate `WithDefaultValue()` for NOT NULL columns
- Use `SystemMethods.CurrentDateTime` for timestamp defaults
- Consider database-specific defaults with `IfDatabase()` conditionals

### Performance Considerations
- Use `Indexed()` for frequently queried columns
- Avoid over-indexing - each index has maintenance overhead
- Consider composite indexes for multi-column queries (see [Indexes](/basics/indexes.md))
- Use appropriate string lengths to optimize storage and performance

### Cross-Database Compatibility
- Test data types across all target database providers
- Use `AsCustom()` with `IfDatabase()` for provider-specific types
- Be aware of different NULL handling across providers, especially with unique constraints
- Consider maximum identifier lengths for different databases (30 for Oracle 12, etc)

### Using SetExistingRowsTo
- Only use `SetExistingRowsTo` when adding new columns to existing tables with data
- Always provide values compatible with the column's data type
- Consider database-specific limitations when using NOT NULL columns
- Use appropriate default values that make business sense for your domain
- Test the migration with actual data to ensure it works as expected

