# SQL Server Provider

FluentMigrator provides comprehensive support for Microsoft SQL Server, including specific features and optimizations for different versions.

## Supported Versions

FluentMigrator supports:
- **SQL Server 2019** ✅ (Recommended)
- **SQL Server 2017** ✅
- **SQL Server 2016** ✅
- **SQL Server 2014** ✅
- **SQL Server 2012** ✅
- **SQL Server 2008/R2** ⚠️ (Limited support)
- **Azure SQL Database** ✅
- **Azure SQL Managed Instance** ✅

## Installation

Install the SQL Server provider package:

```xml
<PackageReference Include="FluentMigrator.Runner.SqlServer" Version="6.2.0" />
```

## Configuration

### Basic Configuration
```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSqlServer()
        .WithGlobalConnectionString("Server=.;Database=MyApp;Trusted_Connection=true;")
        .ScanIn(typeof(MyMigration).Assembly).For.Migrations());
```

### Connection String Examples

#### Windows Authentication
```csharp
"Server=.;Database=MyApp;Trusted_Connection=true;"
"Server=localhost;Database=MyApp;Integrated Security=true;"
```

#### SQL Server Authentication
```csharp
"Server=.;Database=MyApp;User Id=myuser;Password=mypass;"
```

#### Azure SQL Database
```csharp
"Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyApp;Persist Security Info=False;User ID=myuser;Password=mypass;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

#### Connection String with Advanced Options
```csharp
"Server=.;Database=MyApp;Trusted_Connection=true;MultipleActiveResultSets=true;Connection Timeout=60;"
```

## SQL Server Specific Features

### Identity Columns

#### Standard Identity
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(100).NotNullable();
```

#### Custom Identity Seed and Increment
```csharp
Create.Table("Orders")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity(1000, 5) // Start at 1000, increment by 5
    .WithColumn("OrderNumber").AsString(20).NotNullable();
```

### Unique Identifiers (GUIDs)

#### GUID Columns
```csharp
Create.Table("Documents")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
    .WithColumn("Title").AsString(255).NotNullable();
```

#### Sequential GUID (Better Performance)
```csharp
Create.Table("Logs")
    .WithColumn("Id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewSequentialId)
    .WithColumn("Message").AsString().NotNullable();
```

### Data Types

#### SQL Server Specific Types
```csharp
Create.Table("SqlServerTypes")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("XmlData").AsXml().Nullable()                    // XML
    .WithColumn("JsonData").AsString().Nullable()               // NVARCHAR for JSON
    .WithColumn("HierarchyData").AsCustom("HIERARCHYID").Nullable()
    .WithColumn("GeographyData").AsCustom("GEOGRAPHY").Nullable()
    .WithColumn("GeometryData").AsCustom("GEOMETRY").Nullable()
    .WithColumn("MoneyValue").AsCurrency().NotNullable()        // MONEY
    .WithColumn("SmallMoneyValue").AsCustom("SMALLMONEY").Nullable()
    .WithColumn("TimestampValue").AsCustom("ROWVERSION").NotNullable();
```

#### Precision and Scale
```csharp
Create.Table("PrecisionExamples")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("DecimalValue").AsDecimal(18, 4).NotNullable()  // DECIMAL(18,4)
    .WithColumn("FloatValue").AsFloat(53).NotNullable()         // FLOAT(53)
    .WithColumn("RealValue").AsFloat(24).NotNullable();         // REAL
```

### Indexes

#### Clustered and Non-Clustered Indexes
```csharp
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Username").AsString(50).NotNullable()
    .WithColumn("Email").AsString(255).NotNullable()
    .WithColumn("LastName").AsString(100).NotNullable()
    .WithColumn("FirstName").AsString(100).NotNullable();

// Clustered index (primary key is clustered by default)
Create.Index("IX_Users_Username").OnTable("Users")
    .OnColumn("Username").Ascending()
    .WithOptions().Clustered();

// Non-clustered index
Create.Index("IX_Users_Email").OnTable("Users")
    .OnColumn("Email").Ascending()
    .WithOptions().NonClustered();
```

#### Included Columns (Covering Indexes)
```csharp
using FluentMigrator.SqlServer;

Create.Index("IX_Users_LastName_Covering").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .WithOptions().NonClustered()
    .Include("FirstName")
    .Include("Email");
```

#### Filtered Indexes
```csharp
Create.Index("IX_Users_Active").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .WithOptions().NonClustered()
    .WithOptions().Filter("[IsActive] = 1");
```

#### Index Options
```csharp
Create.Index("IX_Users_Complex").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .OnColumn("FirstName").Ascending()
    .WithOptions()
        .NonClustered()
        .WithFillFactor(80)
        .WithPadIndex()
        .WithIgnoreDuplicateKeys();
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

### Schemas

#### Creating and Using Schemas
```csharp
// Create schema
Create.Schema("hr");

// Create table in schema
Create.Table("Employees").InSchema("hr")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(100).NotNullable();

// Cross-schema foreign key
Create.Table("Departments").InSchema("hr")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(100).NotNullable();

Alter.Table("Employees").InSchema("hr")
    .AddColumn("DepartmentId").AsInt32().NotNullable()
    .ForeignKey("FK_Employees_Departments", "hr", "Departments", "Id");
```

### Sequences

#### Creating Sequences
```csharp
Create.Sequence("OrderNumberSeq")
    .InSchema("dbo")
    .StartWith(1000)
    .IncrementBy(1)
    .MinValue(1000)
    .MaxValue(999999)
    .Cache(50);
```

#### Using Sequences in Tables
```csharp
Create.Table("Orders")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("OrderNumber").AsInt32().NotNullable()
        .WithDefaultValue(RawSql.Insert("NEXT VALUE FOR OrderNumberSeq"));
```

### SQL Server Extensions

#### Check Constraints
```csharp
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Price").AsDecimal(10, 2).NotNullable()
    .WithColumn("Quantity").AsInt32().NotNullable();

Create.CheckConstraint("CK_Products_Price_Positive")
    .OnTable("Products")
    .Expression("Price > 0");

Create.CheckConstraint("CK_Products_Quantity_NonNegative")
    .OnTable("Products")
    .Expression("Quantity >= 0");
```

#### Triggers (via SQL)
```csharp
Execute.Sql(@"
CREATE TRIGGER tr_Users_Audit 
ON Users 
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    -- Audit logic here
    INSERT INTO AuditLog (TableName, Action, ModifiedBy, ModifiedAt)
    SELECT 'Users', 'Modified', SYSTEM_USER, GETDATE()
END");
```

### Data Seeding

#### Insert Data
```csharp
Insert.IntoTable("Users")
    .Row(new { Username = "admin", Email = "admin@company.com", IsActive = true })
    .Row(new { Username = "user1", Email = "user1@company.com", IsActive = true });
```

#### Insert from Select
```csharp
Execute.Sql(@"
INSERT INTO UserArchive (Username, Email, ArchivedAt)
SELECT Username, Email, GETDATE()
FROM Users
WHERE IsActive = 0");
```

## Conditional Logic

### SQL Server Version Detection
```csharp
[Migration(1)]
public class SqlServerVersionSpecificMigration : Migration
{
    public override void Up()
    {
        // Feature available in SQL Server 2016+
        if (ApplicationContext.Connection.ServerVersion.Contains("2016") ||
            ApplicationContext.Connection.ServerVersion.Contains("2017") ||
            ApplicationContext.Connection.ServerVersion.Contains("2019"))
        {
            Execute.Sql("ALTER DATABASE CURRENT SET TEMPORAL_HISTORY_RETENTION ON");
        }
    }

    public override void Down()
    {
        // Rollback logic
    }
}
```

### Azure SQL Database Specific
```csharp
IfDatabase(ProcessorIdConstants.SqlServer)
    .Create.Table("AzureSpecific")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Data").AsString().NotNullable();
```

## Performance Considerations

### Batch Size Configuration
```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSqlServer()
        .WithGlobalConnectionString(connectionString)
        .WithGlobalCommandTimeout(TimeSpan.FromMinutes(10)))
    .Configure<ProcessorOptions>(options =>
    {
        options.Timeout = TimeSpan.FromMinutes(10);
    });
```

### Large Table Operations
```csharp
[Migration(1)]
public class LargeTableMigration : Migration
{
    public override void Up()
    {
        // For large tables, consider batch operations
        Execute.Sql(@"
            WHILE @@ROWCOUNT > 0
            BEGIN
                UPDATE TOP (1000) Users 
                SET IsActive = 1 
                WHERE IsActive IS NULL
            END");
    }

    public override void Down()
    {
        // Rollback
    }
}
```

## Best Practices

### 1. Use Appropriate Data Types
```csharp
// Good - specific size for better performance
.WithColumn("Status").AsString(20).NotNullable()

// Avoid - unlimited size can impact performance
.WithColumn("Status").AsString().NotNullable()
```

### 2. Create Indexes for Foreign Keys
```csharp
Create.Table("Orders")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("UserId").AsInt32().NotNullable()
        .ForeignKey("FK_Orders_Users", "Users", "Id");

// Add index on foreign key for better join performance
Create.Index("IX_Orders_UserId").OnTable("Orders").OnColumn("UserId");
```

### 3. Use Schemas for Organization
```csharp
Create.Schema("sales");
Create.Schema("inventory");
Create.Schema("hr");

Create.Table("Products").InSchema("inventory");
Create.Table("Orders").InSchema("sales");
Create.Table("Employees").InSchema("hr");
```

### 4. Handle Connection Timeouts
```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSqlServer()
        .WithGlobalConnectionString(connectionString)
        .WithGlobalCommandTimeout(TimeSpan.FromMinutes(30))); // Increase timeout for long operations
```

## Common Issues and Solutions

### Issue: Timeout Errors on Large Operations
**Solution**: Increase command timeout and use batch operations
```csharp
services.Configure<ProcessorOptions>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(30);
});
```

### Issue: Cannot Drop Column Due to Constraints
**Solution**: Drop constraints first
```csharp
public override void Up()
{
    Delete.DefaultConstraint().OnTable("Users").OnColumn("Status");
    Delete.Column("Status").FromTable("Users");
}
```

### Issue: Identity Insert Issues
**Solution**: Use explicit identity management
```csharp
Execute.Sql("SET IDENTITY_INSERT Users ON");
Insert.IntoTable("Users").Row(new { Id = 1, Name = "Admin" });
Execute.Sql("SET IDENTITY_INSERT Users OFF");
```

## Azure SQL Database Considerations

### Differences from On-Premises SQL Server
- No physical file operations
- Limited administrative commands
- Different pricing tiers affect performance
- Automatic backup and recovery

### Azure-Specific Configuration
```csharp
// Connection string for Azure SQL Database
"Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyApp;Persist Security Info=False;User ID=myuser;Password=mypass;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;"
```

## Next Steps

- [PostgreSQL Provider](./postgresql.md) - Learn about PostgreSQL-specific features
- [Database Provider Comparison](./others.md) - Compare features across providers
- [Advanced Topics](../advanced/dbms-extensions.md) - Explore DBMS extensions