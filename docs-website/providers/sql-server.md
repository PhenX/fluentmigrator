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

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.SqlServer

# For Package Manager Console
Install-Package FluentMigrator.Runner.SqlServer
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
## SQL Server Extensions Package

For advanced SQL Server features, install the extensions package:

```xml
<PackageReference Include="FluentMigrator.Extensions.SqlServer" Version="7.2.0" />
```

## SQL Server Specific Features

### Identity Columns

#### Custom Identity Seed and Increment
```csharp
Create.Table("Orders")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity(1000, 5) // Start at 1000, increment by 5
    .WithColumn("OrderNumber").AsString(20).NotNullable();

// Advanced identity options
Create.Table("Invoices")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
        .Identity(1000, 5) // Start at 1000, increment by 5
    .WithColumn("InvoiceNumber").AsString(20).NotNullable();
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

Column types are specified in the DBMS specific type map classes :

* [SQL Server 2000](https://github.com/fluentmigrator/fluentmigrator/blob/main/src/FluentMigrator.Runner.SqlServer/Generators/SqlServer/SqlServer2000TypeMap.cs)
* [SQL Server 2005](https://github.com/fluentmigrator/fluentmigrator/blob/main/src/FluentMigrator.Runner.SqlServer/Generators/SqlServer/SqlServer2005TypeMap.cs)
* [SQL Server 2008+](https://github.com/fluentmigrator/fluentmigrator/blob/main/src/FluentMigrator.Runner.SqlServer/Generators/SqlServer/SqlServer2008TypeMap.cs)

### Indexes and Index Extensions

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
    .Include("Email")
    .Include("PhoneNumber");

// Complex covering index
Create.Index("IX_Orders_Covering").OnTable("Orders")
    .OnColumn("CustomerId").Ascending()
    .OnColumn("OrderDate").Descending()
    .WithOptions().NonClustered()
    .Include("TotalAmount")
    .Include("Status")
    .Include("ShippingAddress");
```

#### Filtered Indexes
```csharp
Create.Index("IX_Users_Active").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .WithOptions().NonClustered()
    .Filter("[IsActive] = 1");

Create.Index("IX_Orders_ActiveOnly").OnTable("Orders")
    .OnColumn("OrderDate").Descending()
    .WithOptions()
        .NonClustered()
        .Filter("[Status] = 'Active'");
```

#### Advanced Index Options
```csharp
Create.Index("IX_Users_Complex").OnTable("Users")
    .OnColumn("LastName").Ascending()
    .OnColumn("FirstName").Ascending()
    .WithOptions()
        .NonClustered()
        .WithFillFactor(80)
        .WithPadIndex()
        .WithIgnoreDuplicateKeys();

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

#### Column Store Indexes (SQL Server 2012+)
```csharp
// Clustered column store index (for data warehousing)
Create.Index("CIX_Sales_ColumnStore").OnTable("Sales")
    .WithOptions().Clustered().ColumnStore();

// Non-clustered column store with specific columns
Create.Index("NCIX_Sales_Partial").OnTable("Sales")
    .OnColumn("ProductId")
    .OnColumn("SaleDate")
    .OnColumn("Amount")
    .WithOptions().NonClustered().ColumnStore();
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

### Issue: Identity Insert Issues
**Solution**: Use explicit identity management
```csharp
Execute.Sql("SET IDENTITY_INSERT Users ON");
Insert.IntoTable("Users").Row(new { Id = 1, Name = "Admin" });
Execute.Sql("SET IDENTITY_INSERT Users OFF");
```

### Temporal Tables (SQL Server 2016+)

Temporal tables provide automatic versioning of data changes:

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

#### Working with Temporal Tables
```csharp
// Query temporal data
Execute.Sql(@"
-- Current data
SELECT * FROM Users;

-- Data as of specific time
SELECT * FROM Users FOR SYSTEM_TIME AS OF '2023-01-01 00:00:00';

-- Data between time periods
SELECT * FROM Users FOR SYSTEM_TIME BETWEEN '2023-01-01' AND '2023-12-31';

-- All historical data
SELECT * FROM Users FOR SYSTEM_TIME ALL;
");
```

## Azure SQL Database Considerations

### Differences from On-Premises SQL Server
- No physical file operations
- Limited administrative commands
- Different pricing tiers affect performance
- Automatic backup and recovery

## Next Steps

- [PostgreSQL Provider](./postgresql.md) - Learn about PostgreSQL-specific features
- [Database Provider Comparison](./others.md) - Compare features across providers
- [Advanced Topics](../advanced/best-practices.md) - Explore migration best practices
