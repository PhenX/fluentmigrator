# Other Database Providers

FluentMigrator supports a wide range of database providers beyond the major ones (SQL Server, PostgreSQL, MySQL, SQLite, Oracle). This guide covers additional database providers and how to work with them.

## Supported Database Providers

### Firebird

Firebird is an open-source relational database management system.

#### Installation

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.Firebird

# For Package Manager Console
Install-Package FluentMigrator.Runner.Firebird
```

#### Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddFirebird()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

#### Connection String Examples

```csharp
// Embedded server
"User=SYSDBA;Password=masterkey;Database=C:\\data\\mydatabase.fdb;ServerType=1;"

// Network server
"User=SYSDBA;Password=masterkey;Database=localhost:C:\\data\\mydatabase.fdb;ServerType=0;"

// With charset
"User=SYSDBA;Password=masterkey;Database=localhost/3050:C:\\data\\mydatabase.fdb;Charset=UTF8;"
```

#### Firebird-Specific Features

```csharp
public class FirebirdMigration : Migration
{
    public override void Up()
    {
        Create.Table("FirebirdTable")
            .WithColumn("ID").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("NAME").AsString(100).NotNullable()
            .WithColumn("AMOUNT").AsDecimal(15, 2).NotNullable()
            .WithColumn("CREATED_AT").AsDateTime().NotNullable();
            
        // Create generator (sequence) for auto-increment
        Execute.Sql("CREATE GENERATOR GEN_FIREBIRDTABLE_ID");
        Execute.Sql("SET GENERATOR GEN_FIREBIRDTABLE_ID TO 0");
        
        // Create trigger for auto-increment
        Execute.Sql(@"
            CREATE TRIGGER TRG_FIREBIRDTABLE_BI FOR FirebirdTable
            ACTIVE BEFORE INSERT POSITION 0
            AS
            BEGIN
                IF (NEW.ID IS NULL) THEN
                    NEW.ID = GEN_ID(GEN_FIREBIRDTABLE_ID, 1);
            END");
            
        // Firebird specific data types
        Execute.Sql(@"
            ALTER TABLE FirebirdTable 
            ADD BLOB_FIELD BLOB SUB_TYPE TEXT");
            
        Execute.Sql(@"
            ALTER TABLE FirebirdTable 
            ADD TIMESTAMP_FIELD TIMESTAMP");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER TRG_FIREBIRDTABLE_BI");
        Execute.Sql("DROP GENERATOR GEN_FIREBIRDTABLE_ID");
        Delete.Table("FirebirdTable");
    }
}
```

### IBM DB2

IBM DB2 is an enterprise-class database management system.

#### Installation

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.DB2

# For Package Manager Console
Install-Package FluentMigrator.Runner.DB2
```

#### Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddDb2()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

#### Connection String Examples

```csharp
// Local database
"Server=localhost:50000;Database=MYDB;UID=myuser;PWD=mypassword;"

// Remote database with SSL
"Server=remote.server.com:50001;Database=MYDB;UID=myuser;PWD=mypassword;Security=SSL;"

// Connection pooling
"Server=localhost:50000;Database=MYDB;UID=myuser;PWD=mypassword;Pooling=true;Max Pool Size=100;"
```

#### DB2-Specific Features

```csharp
public class Db2Migration : Migration
{
    public override void Up()
    {
        Create.Table("DB2_EMPLOYEES")
            .WithColumn("EMP_ID").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("FIRST_NAME").AsString(50).NotNullable()
            .WithColumn("LAST_NAME").AsString(50).NotNullable()
            .WithColumn("EMAIL").AsString(255).NotNullable()
            .WithColumn("HIRE_DATE").AsDateTime().NotNullable()
            .WithColumn("SALARY").AsDecimal(15, 2).NotNullable();
            
        // Create sequence for auto-increment
        Execute.Sql("CREATE SEQUENCE SEQ_EMP_ID START WITH 1 INCREMENT BY 1 NO CACHE");
        
        // Create trigger for auto-increment
        Execute.Sql(@"
            CREATE TRIGGER TRG_DB2_EMPLOYEES_BI
            BEFORE INSERT ON DB2_EMPLOYEES
            REFERENCING NEW AS N
            FOR EACH ROW
            BEGIN ATOMIC
                SET N.EMP_ID = NEXT VALUE FOR SEQ_EMP_ID;
            END");
            
        // DB2 specific features
        Create.Table("DB2_DOCUMENTS")
            .WithColumn("DOC_ID").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("TITLE").AsString(200).NotNullable()
            .WithColumn("CONTENT").AsCustom("CLOB(1M)").Nullable()
            .WithColumn("CREATED_TS").AsCustom("TIMESTAMP").NotNullable();
            
        // Create indexes
        Create.Index("IX_DB2_EMPLOYEES_EMAIL")
            .OnTable("DB2_EMPLOYEES")
            .OnColumn("EMAIL")
            .Unique();
            
        Create.Index("IX_DB2_EMPLOYEES_NAME")
            .OnTable("DB2_EMPLOYEES")
            .OnColumn("LAST_NAME")
            .OnColumn("FIRST_NAME");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER TRG_DB2_EMPLOYEES_BI");
        Execute.Sql("DROP SEQUENCE SEQ_EMP_ID");
        Delete.Table("DB2_DOCUMENTS");
        Delete.Table("DB2_EMPLOYEES");
    }
}
```

### SAP HANA

SAP HANA is an in-memory database platform.

#### Installation

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.Hana

# For Package Manager Console
Install-Package FluentMigrator.Runner.Hana
```

#### Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddHana()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

#### Connection String Examples

```csharp
// Basic connection
"Server=hanaserver:30015;UserID=myuser;Password=mypassword;DatabaseName=HDB;"

// With encryption
"Server=hanaserver:30015;UserID=myuser;Password=mypassword;DatabaseName=HDB;encrypt=true;"

// Connection pooling
"Server=hanaserver:30015;UserID=myuser;Password=mypassword;DatabaseName=HDB;Pooling=true;Max Pool Size=50;"
```

#### HANA-Specific Features

```csharp
public class HanaMigration : Migration
{
    public override void Up()
    {
        Create.Table("HANA_SALES_DATA")
            .WithColumn("ID").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("CUSTOMER_ID").AsInt32().NotNullable()
            .WithColumn("PRODUCT_NAME").AsString(100).NotNullable()
            .WithColumn("QUANTITY").AsInt32().NotNullable()
            .WithColumn("UNIT_PRICE").AsDecimal(15, 4).NotNullable()
            .WithColumn("SALE_DATE").AsDateTime().NotNullable()
            .WithColumn("REGION").AsString(50).NotNullable();
            
        // HANA sequence
        Execute.Sql("CREATE SEQUENCE SEQ_HANA_SALES_DATA START WITH 1");
        
        // Column store table (HANA default)
        Execute.Sql("ALTER TABLE HANA_SALES_DATA ADD (NOTES NCLOB)");
        
        // Create column store indexes
        Execute.Sql("CREATE INDEX IX_HANA_SALES_CUSTOMER ON HANA_SALES_DATA (CUSTOMER_ID)");
        Execute.Sql("CREATE INDEX IX_HANA_SALES_DATE ON HANA_SALES_DATA (SALE_DATE)");
        
        // Create calculation view (HANA-specific analytical view)
        Execute.Sql(@"
            CREATE VIEW V_SALES_SUMMARY AS 
            SELECT 
                REGION,
                YEAR(SALE_DATE) AS SALE_YEAR,
                MONTH(SALE_DATE) AS SALE_MONTH,
                COUNT(*) AS TRANSACTION_COUNT,
                SUM(QUANTITY * UNIT_PRICE) AS TOTAL_REVENUE
            FROM HANA_SALES_DATA
            GROUP BY REGION, YEAR(SALE_DATE), MONTH(SALE_DATE)");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW V_SALES_SUMMARY");
        Execute.Sql("DROP SEQUENCE SEQ_HANA_SALES_DATA");
        Delete.Table("HANA_SALES_DATA");
    }
}
```

### Redshift

Amazon Redshift is a cloud-based data warehouse service.

#### Installation

```bash
# For .NET CLI
dotnet add package FluentMigrator.Runner.Redshift

# For Package Manager Console
Install-Package FluentMigrator.Runner.Redshift
```

#### Configuration

```csharp
services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddRedshift()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());
```

#### Connection String Examples

```csharp
// Basic Redshift connection
"Server=mycluster.abc123.us-west-2.redshift.amazonaws.com;Port=5439;Database=mydb;User Id=myuser;Password=mypassword;"

// With SSL
"Server=mycluster.abc123.us-west-2.redshift.amazonaws.com;Port=5439;Database=mydb;User Id=myuser;Password=mypassword;SSL=true;"

// Connection timeout
"Server=mycluster.abc123.us-west-2.redshift.amazonaws.com;Port=5439;Database=mydb;User Id=myuser;Password=mypassword;Timeout=30;"
```

#### Redshift-Specific Features

```csharp
public class RedshiftMigration : Migration
{
    public override void Up()
    {
        // Redshift table with distribution and sort keys
        Execute.Sql(@"
            CREATE TABLE sales_fact (
                sale_id BIGINT IDENTITY(1,1) PRIMARY KEY,
                customer_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                sale_date DATE NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL,
                total_amount DECIMAL(15,2) NOT NULL,
                region VARCHAR(50) NOT NULL
            )
            DISTKEY(customer_id)
            SORTKEY(sale_date, customer_id)");
            
        // Dimension table - small table, replicate across all nodes
        Create.Table("dim_regions")
            .WithColumn("region_id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("region_name").AsString(100).NotNullable()
            .WithColumn("region_code").AsString(10).NotNullable()
            .WithColumn("country").AsString(100).NotNullable();
            
        Execute.Sql("ALTER TABLE dim_regions ADD DISTSTYLE ALL");
        
        // Create external table for S3 data
        Execute.Sql(@"
            CREATE EXTERNAL TABLE external_customer_data (
                customer_id INTEGER,
                customer_name VARCHAR(200),
                email VARCHAR(255),
                registration_date DATE
            )
            STORED AS PARQUET
            LOCATION 's3://my-bucket/customer-data/'
            TABLE PROPERTIES ('numRows'='1000000')");
            
        // Create materialized view for aggregations
        Execute.Sql(@"
            CREATE MATERIALIZED VIEW mv_monthly_sales AS
            SELECT 
                DATE_TRUNC('month', sale_date) as month_year,
                region,
                COUNT(*) as transaction_count,
                SUM(total_amount) as total_revenue,
                AVG(total_amount) as avg_transaction_value
            FROM sales_fact
            GROUP BY DATE_TRUNC('month', sale_date), region");
    }

    public override void Down()
    {
        Execute.Sql("DROP MATERIALIZED VIEW IF EXISTS mv_monthly_sales");
        Execute.Sql("DROP TABLE IF EXISTS external_customer_data");
        Delete.Table("dim_regions");
        Execute.Sql("DROP TABLE IF EXISTS sales_fact");
    }
}
```

## Generic Database Provider Support

### Using ODBC Connections

For databases not directly supported, you can use ODBC connections:

```csharp
public class OdbcMigration : Migration
{
    public override void Up()
    {
            IfDatabase("Generic").Execute.Sql(@"
                CREATE TABLE generic_table (
                    id INTEGER NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    amount DECIMAL(10,2),
                    created_date TIMESTAMP
                )");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE generic_table");
    }
}
```

### Connection String for ODBC

```csharp
// Generic ODBC connection
"Driver={SQL Server};Server=myServer;Database=myDatabase;Uid=myUser;Pwd=myPassword;"

// For other databases
"DSN=MyDataSource;UID=myUser;PWD=myPassword;"
```

## Cross-Database Compatibility

### Writing Database-Agnostic Migrations

```csharp
public class CrossDatabaseMigration : Migration
{
    public override void Up()
    {
        // Create table using FluentMigrator abstractions
        Create.Table("CrossDbTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Create indexes using FluentMigrator
        Create.Index("IX_CrossDbTable_Email")
            .OnTable("CrossDbTable")
            .OnColumn("Email")
            .Unique();
            
        Create.Index("IX_CrossDbTable_Name")
            .OnTable("CrossDbTable")
            .OnColumn("Name");
            
        // Database-specific optimizations
            IfDatabase("SqlServer").Execute.Sql("CREATE NONCLUSTERED INDEX IX_CrossDbTable_Amount_Filtered ON CrossDbTable (Amount) WHERE Amount > 0");
    IfDatabase("Postgres").Execute.Sql("CREATE INDEX IX_CrossDbTable_Amount_Partial ON CrossDbTable (Amount) WHERE Amount > 0");
    IfDatabase("MySQL").Execute.Sql("ALTER TABLE CrossDbTable ENGINE=InnoDB");
    IfDatabase("Oracle").Execute.Sql("CREATE INDEX IX_CrossDbTable_Amount_Filtered ON CrossDbTable (Amount) WHERE Amount > 0");
    }

    public override void Down()
    {
        Delete.Table("CrossDbTable");
    }
}
```

### Handling Data Type Differences

```csharp
public class DataTypeCompatibilityMigration : Migration
{
    public override void Up()
    {
        Create.Table("CompatibilityTest")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("LongText").AsString(int.MaxValue).Nullable() // Will map appropriately per DB
            .WithColumn("BinaryData").AsBinary().Nullable()
            .WithColumn("UniqueId").AsGuid().NotNullable()
            .WithColumn("Timestamp").AsDateTime().NotNullable();
            
        // Handle database-specific data types
            IfDatabase("SqlServer").Execute.Sql("ALTER TABLE CompatibilityTest ADD XmlData XML");
    IfDatabase("Postgres").Execute.Sql("ALTER TABLE CompatibilityTest ADD JsonData JSONB");
    IfDatabase("MySQL").Execute.Sql("ALTER TABLE CompatibilityTest ADD JsonData JSON");
    IfDatabase("Oracle").Execute.Sql("ALTER TABLE CompatibilityTest ADD XmlData XMLType");
        else
        {
            // Generic fallback
            Execute.Sql("ALTER TABLE CompatibilityTest ADD ExtendedData TEXT");
        }
    }

    public override void Down()
    {
        Delete.Table("CompatibilityTest");
    }
}
```

## Best Practices for Multiple Database Support

### Configuration Management

```csharp
public static class DatabaseConfiguration
{
    public static void ConfigureForEnvironment(IServiceCollection services, string environment)
    {
        var connectionString = GetConnectionString(environment);
        var databaseType = GetDatabaseType(environment);
        
        var builder = services.AddFluentMigratorCore()
            .ConfigureRunner(rb => {
                switch (databaseType.ToLower())
                {
                    case "sqlserver":
                        rb.AddSqlServer();
                        break;
                    case "postgresql":
                        rb.AddPostgres();
                        break;
                    case "mysql":
                        rb.AddMySql();
                        break;
                    case "sqlite":
                        rb.AddSQLite();
                        break;
                    case "oracle":
                        rb.AddOracle();
                        break;
                    case "firebird":
                        rb.AddFirebird();
                        break;
                    case "db2":
                        rb.AddDb2();
                        break;
                    default:
                        throw new NotSupportedException($"Database type {databaseType} is not supported");
                }
                
                rb.WithGlobalConnectionString(connectionString)
                  .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations();
            })
            .AddLogging(lb => lb.AddFluentMigratorConsole());
    }
    
    private static string GetConnectionString(string environment)
    {
        // Implementation to get connection string based on environment
        return Environment.GetEnvironmentVariable($"DATABASE_CONNECTION_{environment.ToUpper()}") 
               ?? throw new InvalidOperationException($"Connection string for {environment} not found");
    }
    
    private static string GetDatabaseType(string environment)
    {
        // Implementation to get database type based on environment
        return Environment.GetEnvironmentVariable($"DATABASE_TYPE_{environment.ToUpper()}") 
               ?? "SqlServer";
    }
}
```

### Testing Across Multiple Databases

```csharp
public class MultiDatabaseTestMigration : Migration
{
    public override void Up()
    {
        // Create base structure that works across all databases
        Create.Table("TestTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Value").AsDecimal(10, 2).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // Add test data that works across databases
        Insert.IntoTable("TestTable")
            .Row(new { Name = "Test 1", Value = 100.50m, CreatedAt = DateTime.Now })
            .Row(new { Name = "Test 2", Value = 200.75m, CreatedAt = DateTime.Now })
            .Row(new { Name = "Test 3", Value = 300.25m, CreatedAt = DateTime.Now });
            
        // Verify data can be read back
        var count = Execute.Sql("SELECT COUNT(*) FROM TestTable").Returns<int>().FirstOrDefault();
        
        if (count != 3)
        {
            throw new InvalidOperationException($"Expected 3 records, found {count}");
        }
    }

    public override void Down()
    {
        Delete.Table("TestTable");
    }
}
```

## Troubleshooting Multi-Database Issues

### Common Issues and Solutions

```csharp
public class TroubleshootingMigration : Migration
{
    public override void Up()
    {
        try
        {
            // Issue 1: Different identifier quoting
            var tableName = IfDatabase("MySQL") ? "`test_table`" : 
                           IfDatabase("Postgres") ? "\"test_table\"" :
                           IfDatabase("SqlServer") ? "[test_table]" :
                           "test_table";
                           
            // Use FluentMigrator abstractions instead
            Create.Table("test_table")
                .WithColumn("id").AsInt32().NotNullable().PrimaryKey()
                .WithColumn("name").AsString(100).NotNullable();
                
            // Issue 2: Different date/time handling
                IfDatabase("MySQL").Execute.Sql("ALTER TABLE test_table ADD COLUMN created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP");
    IfDatabase("Postgres").Execute.Sql("ALTER TABLE test_table ADD COLUMN created_at TIMESTAMP DEFAULT NOW()");
    IfDatabase("SqlServer").Execute.Sql("ALTER TABLE test_table ADD created_at DATETIME2 DEFAULT GETDATE()");
            else
            {
                // Generic fallback
                Execute.Sql("ALTER TABLE test_table ADD created_at DATETIME");
            }
            
            // Issue 3: Different auto-increment syntax
                IfDatabase("MySQL").Execute.Sql("ALTER TABLE test_table MODIFY id INT AUTO_INCREMENT");
    IfDatabase("Postgres").Execute.Sql("ALTER TABLE test_table ALTER COLUMN id SET DEFAULT nextval('test_table_id_seq')");
    IfDatabase("Oracle").Execute.Sql("CREATE SEQUENCE seq_test_table START WITH 1");
        }
        catch (Exception ex)
        {
            Execute.Sql($"-- Error in migration: {ex.Message}");
            throw;
        }
    }

    public override void Down()
    {
            IfDatabase("Oracle").Execute.Sql("DROP TRIGGER tr_test_table_bi");
        
        Delete.Table("test_table");
    }
}
```

### Performance Considerations

```csharp
public class PerformanceOptimizationMigration : Migration
{
    public override void Up()
    {
        Create.Table("PerformanceTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Amount").AsDecimal(15, 2).NotNullable()
            .WithColumn("CreatedDate").AsDateTime().NotNullable();
            
        // Database-specific performance optimizations
            IfDatabase("SqlServer").Execute.Sql("CREATE INDEX IX_PerformanceTable_Status_Active ON PerformanceTable (Status) WHERE Status = 'Active'");
    IfDatabase("Postgres").Execute.Sql("CREATE INDEX IX_PerformanceTable_Status_Active ON PerformanceTable (Status) WHERE Status = 'Active'");
    IfDatabase("MySQL").Execute.Sql("ALTER TABLE PerformanceTable ENGINE=InnoDB");
    IfDatabase("Oracle").Execute.Sql("CREATE BITMAP INDEX IX_PerformanceTable_Status_BM ON PerformanceTable (Status)");
        
        // Generic indexes that work across databases
        Create.Index("IX_PerformanceTable_CategoryId")
            .OnTable("PerformanceTable")
            .OnColumn("CategoryId");
            
        Create.Index("IX_PerformanceTable_CreatedDate")
            .OnTable("PerformanceTable")
            .OnColumn("CreatedDate");
    }

    public override void Down()
    {
        Delete.Table("PerformanceTable");
    }
}
```

## See Also

- [SQL Server Provider](./sql-server.md)
- [PostgreSQL Provider](./postgresql.md)
- [MySQL Provider](./mysql.md)
- [SQLite Provider](./sqlite.md)
- [Oracle Provider](./oracle.md)
- [Installation Guide](../installation.md)
- [Best Practices](../advanced/best-practices.md)
- [Troubleshooting](../advanced/edge-cases.md)