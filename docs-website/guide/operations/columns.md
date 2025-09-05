# Managing Columns

This guide covers comprehensive column management in FluentMigrator, including creation, modification, and deletion of database columns with various data types and constraints.

## Column Data Types

### Basic Data Types

```csharp
public class BasicColumnTypes : Migration
{
    public override void Up()
    {
        Create.Table("ExampleTable")
            // String types
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("Code").AsFixedLengthString(10).NotNullable()
            
            // Numeric types
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Count").AsInt64().NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            .WithColumn("Rating").AsFloat().Nullable()
            .WithColumn("Precision").AsDouble().Nullable()
            
            // Date and time types
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().Nullable()
            .WithColumn("BirthDate").AsDate().Nullable()
            .WithColumn("StartTime").AsTime().Nullable()
            
            // Boolean and binary types
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("Data").AsBinary(1024).Nullable()
            .WithColumn("Document").AsBinary().Nullable() // Variable length
            
            // GUID type
            .WithColumn("UniqueId").AsGuid().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ExampleTable");
    }
}
```

### Custom Data Types

```csharp
public class CustomColumnTypes : Migration
{
    public override void Up()
    {
        Create.Table("CustomTypes")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity();
            
        // Database-specific custom types
        if (IfDatabase("SqlServer"))
        {
            Alter.Table("CustomTypes")
                .AddColumn("XmlData").AsCustom("XML").Nullable()
                .AddColumn("JsonData").AsCustom("NVARCHAR(MAX)").Nullable()
                .AddColumn("HierarchyPath").AsCustom("HIERARCHYID").Nullable();
        }
        
        if (IfDatabase("Postgres"))
        {
            Alter.Table("CustomTypes")
                .AddColumn("JsonData").AsCustom("JSONB").Nullable()
                .AddColumn("ArrayData").AsCustom("TEXT[]").Nullable()
                .AddColumn("UuidData").AsCustom("UUID").Nullable();
        }
        
        if (IfDatabase("MySQL"))
        {
            Alter.Table("CustomTypes")
                .AddColumn("JsonData").AsCustom("JSON").Nullable()
                .AddColumn("EnumData").AsCustom("ENUM('small','medium','large')").Nullable()
                .AddColumn("SetData").AsCustom("SET('red','green','blue')").Nullable();
        }
    }

    public override void Down()
    {
        Delete.Table("CustomTypes");
    }
}
```

## Column Constraints

### Primary Key Columns

```csharp
public class PrimaryKeyColumns : Migration
{
    public override void Up()
    {
        // Single column primary key with identity
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
            
        // GUID primary key
        Create.Table("Documents")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Title").AsString(200).NotNullable();
            
        // Composite primary key
        Create.Table("OrderItems")
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable();
            
        Create.PrimaryKey("PK_OrderItems")
            .OnTable("OrderItems")
            .Columns("OrderId", "ProductId");
    }

    public override void Down()
    {
        Delete.Table("Users");
        Delete.Table("Documents");
        Delete.Table("OrderItems");
    }
}
```

### Nullable and Not Nullable Columns

```csharp
public class NullabilityExamples : Migration
{
    public override void Up()
    {
        Create.Table("Products")
            // Required fields
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            
            // Optional fields
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("DiscountPrice").AsDecimal(10, 2).Nullable()
            .WithColumn("DiscontinuedAt").AsDateTime().Nullable()
            
            // Required with default values
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("Products");
    }
}
```

### Default Values

```csharp
public class DefaultValueExamples : Migration
{
    public override void Up()
    {
        Create.Table("Settings")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            
            // Static default values
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active")
            .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("IsEnabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("Rate").AsDecimal(5, 2).NotNullable().WithDefaultValue(0.00m)
            
            // System method defaults
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("UniqueId").AsGuid().NotNullable().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("Timestamp").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            
            // Custom SQL defaults (database-specific)
            .WithColumn("RandomValue").AsInt32().NotNullable().WithDefaultValue(RawSql.Insert("ABS(CHECKSUM(NEWID()))"));
    }

    public override void Down()
    {
        Delete.Table("Settings");
    }
}
```

## Column Modifications

### Changing Column Data Types

```csharp
public class ChangeColumnTypes : Migration
{
    public override void Up()
    {
        // Expand string column
        Alter.Column("Description").OnTable("Products")
            .AsString(2000).Nullable(); // Changed from 1000 to 2000
            
        // Change numeric precision
        Alter.Column("Price").OnTable("Products")
            .AsDecimal(12, 4).NotNullable(); // Changed from (10,2) to (12,4)
            
        // Convert between compatible types
        Alter.Column("IsActive").OnTable("Products")
            .AsString(10).NotNullable(); // Boolean to string conversion
    }

    public override void Down()
    {
        Alter.Column("Description").OnTable("Products")
            .AsString(1000).Nullable();
            
        Alter.Column("Price").OnTable("Products")
            .AsDecimal(10, 2).NotNullable();
            
        Alter.Column("IsActive").OnTable("Products")
            .AsBoolean().NotNullable();
    }
}
```

### Changing Column Nullability

```csharp
public class ChangeColumnNullability : Migration
{
    public override void Up()
    {
        // Make nullable column not nullable (with default value)
        Alter.Column("Status").OnTable("Orders")
            .AsString(20).NotNullable().WithDefaultValue("Pending");
            
        // Make not nullable column nullable
        Alter.Column("MiddleName").OnTable("Users")
            .AsString(50).Nullable();
    }

    public override void Down()
    {
        Alter.Column("Status").OnTable("Orders")
            .AsString(20).Nullable();
            
        Alter.Column("MiddleName").OnTable("Users")
            .AsString(50).NotNullable();
    }
}
```

### Adding and Removing Default Values

```csharp
public class ModifyDefaultValues : Migration
{
    public override void Up()
    {
        // Add default value to existing column
        Alter.Column("CreatedAt").OnTable("Users")
            .AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Change existing default value
        Alter.Column("Status").OnTable("Products")
            .AsString(20).NotNullable().WithDefaultValue("Draft"); // Changed from "Active"
    }

    public override void Down()
    {
        Alter.Column("CreatedAt").OnTable("Users")
            .AsDateTime().NotNullable(); // Remove default
            
        Alter.Column("Status").OnTable("Products")
            .AsString(20).NotNullable().WithDefaultValue("Active");
    }
}
```

## Advanced Column Features

### Identity Columns

```csharp
public class IdentityColumns : Migration
{
    public override void Up()
    {
        // Standard identity column
        Create.Table("StandardIdentity")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
            
        // Identity with custom seed and increment
        Create.Table("CustomIdentity")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity(1000, 10) // Start at 1000, increment by 10
            .WithColumn("Name").AsString(100).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("StandardIdentity");
        Delete.Table("CustomIdentity");
    }
}
```

### Computed Columns (SQL Server)

```csharp
public class ComputedColumns : Migration
{
    public override void Up()
    {
        if (IfDatabase("SqlServer"))
        {
            Create.Table("Invoices")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Subtotal").AsDecimal(10, 2).NotNullable()
                .WithColumn("TaxRate").AsDecimal(5, 4).NotNullable()
                .WithColumn("Total").AsDecimal(10, 2).Computed("Subtotal + (Subtotal * TaxRate)")
                .WithColumn("FormattedTotal").AsString(20).Computed("FORMAT(Subtotal + (Subtotal * TaxRate), 'C')");
        }
    }

    public override void Down()
    {
        if (IfDatabase("SqlServer"))
        {
            Delete.Table("Invoices");
        }
    }
}
```

### Column Descriptions and Comments

```csharp
public class ColumnDescriptions : Migration
{
    public override void Up()
    {
        Create.Table("DocumentedTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumnDescription("Primary key identifier")
            .WithColumn("Name").AsString(100).NotNullable()
                .WithColumnDescription("The display name of the entity")
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active")
                .WithColumnDescription("Current status: Active, Inactive, Pending")
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
                .WithColumnDescription("Timestamp when the record was created");
    }

    public override void Down()
    {
        Delete.Table("DocumentedTable");
    }
}
```

## Database-Specific Column Features

### SQL Server Specific Features

```csharp
public class SqlServerSpecificColumns : Migration
{
    public override void Up()
    {
        if (IfDatabase("SqlServer"))
        {
            Create.Table("SqlServerFeatures")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                
                // SQL Server specific data types
                .WithColumn("XmlData").AsCustom("XML").Nullable()
                .WithColumn("HierarchyPath").AsCustom("HIERARCHYID").Nullable()
                .WithColumn("SpatialData").AsCustom("GEOMETRY").Nullable()
                .WithColumn("FileStreamData").AsCustom("VARBINARY(MAX) FILESTREAM").Nullable()
                
                // Columnstore index
                .WithColumn("LargeText").AsCustom("NVARCHAR(MAX)").Nullable()
                
                // Row version for optimistic concurrency
                .WithColumn("RowVersion").AsCustom("ROWVERSION").NotNullable();
        }
    }

    public override void Down()
    {
        if (IfDatabase("SqlServer"))
        {
            Delete.Table("SqlServerFeatures");
        }
    }
}
```

### PostgreSQL Specific Features

```csharp
public class PostgreSqlSpecificColumns : Migration
{
    public override void Up()
    {
        if (IfDatabase("Postgres"))
        {
            Create.Table("PostgreSqlFeatures")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                
                // PostgreSQL specific data types
                .WithColumn("JsonData").AsCustom("JSONB").Nullable()
                .WithColumn("ArrayData").AsCustom("TEXT[]").Nullable()
                .WithColumn("UuidData").AsCustom("UUID").Nullable()
                .WithColumn("InetAddress").AsCustom("INET").Nullable()
                .WithColumn("MacAddress").AsCustom("MACADDR").Nullable()
                
                // Serial types
                .WithColumn("SerialId").AsCustom("SERIAL").NotNullable()
                .WithColumn("BigSerialId").AsCustom("BIGSERIAL").NotNullable()
                
                // Range types
                .WithColumn("DateRange").AsCustom("DATERANGE").Nullable()
                .WithColumn("IntRange").AsCustom("INT4RANGE").Nullable();
        }
    }

    public override void Down()
    {
        if (IfDatabase("Postgres"))
        {
            Delete.Table("PostgreSqlFeatures");
        }
    }
}
```

### MySQL Specific Features

```csharp
public class MySqlSpecificColumns : Migration
{
    public override void Up()
    {
        if (IfDatabase("MySQL"))
        {
            Create.Table("MySqlFeatures")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                
                // MySQL specific data types
                .WithColumn("JsonData").AsCustom("JSON").Nullable()
                .WithColumn("TinyIntData").AsCustom("TINYINT").NotNullable()
                .WithColumn("MediumIntData").AsCustom("MEDIUMINT").NotNullable()
                
                // ENUM and SET types
                .WithColumn("Size").AsCustom("ENUM('small','medium','large','xlarge')").Nullable()
                .WithColumn("Colors").AsCustom("SET('red','green','blue','yellow')").Nullable()
                
                // Text types with specific lengths
                .WithColumn("SmallText").AsCustom("TINYTEXT").Nullable()
                .WithColumn("MediumText").AsCustom("MEDIUMTEXT").Nullable()
                .WithColumn("LongText").AsCustom("LONGTEXT").Nullable()
                
                // Timestamp with automatic update
                .WithColumn("CreatedAt").AsCustom("TIMESTAMP DEFAULT CURRENT_TIMESTAMP").NotNullable()
                .WithColumn("UpdatedAt").AsCustom("TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP").NotNullable();
        }
    }

    public override void Down()
    {
        if (IfDatabase("MySQL"))
        {
            Delete.Table("MySqlFeatures");
        }
    }
}
```

## Column Validation and Constraints

### Check Constraints on Columns

```csharp
public class ColumnCheckConstraints : Migration
{
    public override void Up()
    {
        Create.Table("ValidatedData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Age").AsInt32().NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Rating").AsDecimal(3, 2).Nullable()
            .WithColumn("Status").AsString(20).NotNullable();
            
        // Add check constraints
        Create.CheckConstraint("CK_ValidatedData_Age")
            .OnTable("ValidatedData")
            .WithSql("Age >= 0 AND Age <= 150");
            
        Create.CheckConstraint("CK_ValidatedData_Rating")
            .OnTable("ValidatedData")
            .WithSql("Rating >= 0.0 AND Rating <= 5.0");
            
        Create.CheckConstraint("CK_ValidatedData_Status")
            .OnTable("ValidatedData")
            .WithSql("Status IN ('Active', 'Inactive', 'Pending', 'Suspended')");
    }

    public override void Down()
    {
        Delete.CheckConstraint("CK_ValidatedData_Age").FromTable("ValidatedData");
        Delete.CheckConstraint("CK_ValidatedData_Rating").FromTable("ValidatedData");
        Delete.CheckConstraint("CK_ValidatedData_Status").FromTable("ValidatedData");
        Delete.Table("ValidatedData");
    }
}
```

### Unique Constraints on Columns

```csharp
public class ColumnUniqueConstraints : Migration
{
    public override void Up()
    {
        Create.Table("UniqueData")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("SocialSecurityNumber").AsString(11).Nullable()
            .WithColumn("CompanyId").AsInt32().NotNullable()
            .WithColumn("EmployeeCode").AsString(20).NotNullable();
            
        // Single column unique constraints
        Create.UniqueConstraint("UQ_UniqueData_Email")
            .OnTable("UniqueData")
            .Column("Email");
            
        Create.UniqueConstraint("UQ_UniqueData_Username")
            .OnTable("UniqueData")
            .Column("Username");
            
        // Multi-column unique constraint
        Create.UniqueConstraint("UQ_UniqueData_CompanyEmployee")
            .OnTable("UniqueData")
            .Columns("CompanyId", "EmployeeCode");
    }

    public override void Down()
    {
        Delete.UniqueConstraint("UQ_UniqueData_Email").FromTable("UniqueData");
        Delete.UniqueConstraint("UQ_UniqueData_Username").FromTable("UniqueData");
        Delete.UniqueConstraint("UQ_UniqueData_CompanyEmployee").FromTable("UniqueData");
        Delete.Table("UniqueData");
    }
}
```

## Best Practices for Column Management

### 1. Choose Appropriate Data Types

```csharp
public class DataTypesBestPractices : Migration
{
    public override void Up()
    {
        Create.Table("BestPractices")
            // Use specific sizes for strings to prevent over-allocation
            .WithColumn("Name").AsString(100).NotNullable() // Not AsString() without size
            
            // Use appropriate numeric types
            .WithColumn("SmallCount").AsInt16().NotNullable() // For values < 32,767
            .WithColumn("LargeCount").AsInt64().NotNullable() // For large numbers
            
            // Use decimal for monetary values, not float/double
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            
            // Use DateTime2 instead of DateTime in SQL Server for better precision
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            
            // Use GUID for distributed systems
            .WithColumn("ExternalId").AsGuid().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("BestPractices");
    }
}
```

### 2. Handle Null Values Appropriately

```csharp
public class NullHandlingBestPractices : Migration
{
    public override void Up()
    {
        Create.Table("NullHandling")
            // Required business fields should be NOT NULL
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Pending")
            
            // Optional fields can be NULL
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CompletedDate").AsDateTime().Nullable()
            
            // Use meaningful defaults for system fields
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
    }

    public override void Down()
    {
        Delete.Table("NullHandling");
    }
}
```

### 3. Plan for Data Migration

```csharp
public class DataMigrationPlanning : Migration
{
    public override void Up()
    {
        // Step 1: Add new column as nullable
        Alter.Table("Users")
            .AddColumn("FullName").AsString(200).Nullable();
            
        // Step 2: Populate data
        Execute.Sql(@"
            UPDATE Users 
            SET FullName = COALESCE(FirstName + ' ' + LastName, FirstName, LastName, 'Unknown')
            WHERE FullName IS NULL");
            
        // Step 3: Make column not nullable after data is populated
        Alter.Column("FullName").OnTable("Users")
            .AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("FullName").FromTable("Users");
    }
}
```

## Performance Considerations

### Indexing Strategy for Columns

```csharp
public class ColumnIndexingStrategy : Migration
{
    public override void Up()
    {
        Create.Table("PerformanceOptimized")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable() // Will be indexed for FK
            .WithColumn("Status").AsString(20).NotNullable() // Frequently filtered
            .WithColumn("CreatedAt").AsDateTime().NotNullable() // Range queries
            .WithColumn("Email").AsString(255).NotNullable() // Unique lookups
            .WithColumn("LargeText").AsString(4000).Nullable(); // Not indexed
            
        // Index frequently queried columns
        Create.Index("IX_PerformanceOptimized_UserId")
            .OnTable("PerformanceOptimized")
            .OnColumn("UserId");
            
        Create.Index("IX_PerformanceOptimized_Status_CreatedAt")
            .OnTable("PerformanceOptimized")
            .OnColumn("Status")
            .OnColumn("CreatedAt");
            
        // Unique index for lookups
        Create.UniqueConstraint("UQ_PerformanceOptimized_Email")
            .OnTable("PerformanceOptimized")
            .Column("Email");
    }

    public override void Down()
    {
        Delete.Table("PerformanceOptimized");
    }
}
```

## Troubleshooting Common Column Issues

### Issue: Data Type Conversion Failures

```csharp
public class SafeDataTypeConversion : Migration
{
    public override void Up()
    {
        // First, check and clean data that might cause conversion issues
        Execute.Sql(@"
            UPDATE Products 
            SET Price = NULL 
            WHERE Price = '' OR Price = 'N/A' OR TRY_CONVERT(DECIMAL(10,2), Price) IS NULL");
            
        // Then perform the conversion
        Alter.Column("Price").OnTable("Products")
            .AsDecimal(10, 2).Nullable();
    }

    public override void Down()
    {
        Alter.Column("Price").OnTable("Products")
            .AsString(50).Nullable();
    }
}
```

### Issue: Column Size Reduction

```csharp
public class SafeColumnSizeReduction : Migration
{
    public override void Up()
    {
        // Check for data that would be truncated
        var longDataExists = Execute.Sql(@"
            SELECT COUNT(*) FROM Users WHERE LEN(Description) > 100").Returns<int>().FirstOrDefault();
            
        if (longDataExists > 0)
        {
            throw new InvalidOperationException("Cannot reduce column size - data would be truncated");
        }
        
        // Safe to reduce size
        Alter.Column("Description").OnTable("Users")
            .AsString(100).Nullable();
    }

    public override void Down()
    {
        Alter.Column("Description").OnTable("Users")
            .AsString(500).Nullable();
    }
}
```

## See Also

- [Creating Tables](create-tables.md)
- [Altering Tables](alter-tables.md)
- [Working with Indexes](indexes.md)
- [Foreign Keys](foreign-keys.md)
- [Data Operations](data.md)
- [Best Practices](../advanced/best-practices.md)