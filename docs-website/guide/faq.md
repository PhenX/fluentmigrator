# Frequently Asked Questions

This FAQ answers the most common questions about FluentMigrator. If your question isn't covered here, please [open an issue](https://github.com/fluentmigrator/fluentmigrator/issues) on GitHub.

## Migration Discovery Issues

### Why does the migration tool say "No migrations found"?

**Possible reasons:**
- Migration class isn't **public**
- Migration class doesn't inherit from `IMigration` (or `Migration` base class)
- Migration class isn't attributed with `[Migration(version)]`
- The versions of your migration tool and FluentMigrator packages are different
- Assembly isn't being scanned correctly

**Solutions:**
```csharp
// ✅ Correct migration structure
[Migration(20231201120000)]
public class AddUserTable : Migration  // Must be public and inherit from Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithIdColumn()
            .WithColumn("Name").AsString(100).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

### Why aren't my Maintenance Migrations found by the In-Process Runner?

**Solution**: Use `.For.All()` instead of `.For.Migrations()`:

```csharp
// ❌ Won't find maintenance migrations
.ConfigureRunner(rb => rb
    .AddSqlServer()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(MyMigration).Assembly).For.Migrations())

// ✅ Finds all migration types
.ConfigureRunner(rb => rb
    .AddSqlServer() 
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(MyMigration).Assembly).For.All())
```

## Assembly Loading Issues

### FileLoadException: Could not load assembly 'FluentMigrator...'

**Common scenario**: You installed FluentMigrator.DotNet.Cli globally with one version, but your assembly references a different version.

**Stack trace example:**
```
System.IO.FileLoadException: Could not load file or assembly 'FluentMigrator, Version=3.2.1.0...'
```

**Solutions:**

1. **Use local tool installation** (recommended):
```bash
# Instead of global installation
dotnet tool install FluentMigrator.DotNet.Cli
dotnet tool run dotnet-fm migrate -p sqlserver -c "..." -a "MyApp.dll"
```

2. **Allow dirty assemblies** (temporary fix):
```bash
dotnet fm migrate --allowDirtyAssemblies -p sqlserver -c "..." -a "MyApp.dll"
```

3. **Match versions exactly**:
   - Ensure FluentMigrator NuGet packages and tools have the same version

### How can I run FluentMigrator.DotNet.Cli with different .NET runtime targets?

Use the `--allowDirtyAssemblies` flag:
```bash
dotnet fm migrate --allowDirtyAssemblies -p sqlserver -c "..." -a "MyApp.dll"
```

This allows loading migration assemblies (e.g., .NET 6.0) in a different runtime context (e.g., .NET 5.0).

## Database Support

### What databases are supported?

| Database | Identifier | Alternative Identifiers |
|----------|------------|------------------------|
| **SQL Server 2022** | `SqlServer2016`¹ | `SqlServer` |
| **SQL Server 2019** | `SqlServer2016`² | `SqlServer` |
| **SQL Server 2017** | `SqlServer2016`³ | `SqlServer` |
| **SQL Server 2016** | `SqlServer2016` | `SqlServer` |
| **SQL Server 2014** | `SqlServer2014` | `SqlServer` |
| **SQL Server 2012** | `SqlServer2012` | `SqlServer` |
| **SQL Server 2008** | `SqlServer2008` | `SqlServer` |
| **SQL Server 2005** | `SqlServer2005` | `SqlServer` |
| **PostgreSQL** | `Postgres` | `PostgreSQL` |
| **PostgreSQL 15.0** | `PostgreSQL15_0` | `PostgreSQL` |
| **PostgreSQL 11.0** | `PostgreSQL11_0` | `PostgreSQL` |
| **PostgreSQL 10.0** | `PostgreSQL10_0` | `PostgreSQL` |
| **PostgreSQL 9.2** | `Postgres92` | `PostgreSQL92` |
| **MySQL 8** | `MySQL8` | `MySql`, `MariaDB` |
| **MySQL 5** | `MySql5` | `MySql`, `MariaDB` |
| **MySQL 4** | `MySql4` | `MySql` |
| **Oracle** | `Oracle` | |
| **Oracle (managed)** | `OracleManaged` | `Oracle` |
| **Oracle (DotConnect)** | `OracleDotConnect` | `Oracle` |
| **SQLite** | `Sqlite` | |
| **Firebird** | `Firebird` | |
| **Amazon Redshift** | `Redshift` | |
| **SAP HANA** | `Hana` | |
| **DB2** | `DB2` | |
| **DB2 iSeries** | `DB2 iSeries` | `DB2` |
| **Microsoft JET (Access)** | `Jet` | |

**Notes:**
1. ¹² ³ All integration tests pass using SqlServer2016 dialect
2. SQL Server Compact Edition support dropped (end-of-life)
3. SAP SQL Anywhere support dropped (no .NET Core driver)

## Multi-Server Deployments

### How can I run migrations safely from multiple application servers?

When running multiple instances of your application (load-balanced scenarios), you need to prevent concurrent migration execution.

#### Database-Dependent Application Locking (SQL Server)

**Acquire lock before all migrations:**
```csharp
[Maintenance(MigrationStage.BeforeAll, TransactionBehavior.None)]
public class DbMigrationLockBefore : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            DECLARE @result INT
            EXEC @result = sp_getapplock 'MyApp', 'Exclusive', 'Session'

            IF @result < 0
            BEGIN
                DECLARE @msg NVARCHAR(1000) = 'Received error code ' + 
                    CAST(@result AS VARCHAR(10)) + ' from sp_getapplock during migrations'
                THROW 99999, @msg, 1
            END
        ");
    }

    public override void Down()
    {
        throw new NotImplementedException("Down migrations not supported for sp_getapplock");
    }
}
```

**Release lock after all migrations:**
```csharp
[Maintenance(MigrationStage.AfterAll, TransactionBehavior.None)]
public class DbMigrationUnlockAfter : Migration
{
    public override void Up()
    {
        Execute.Sql("EXEC sp_releaseapplock 'MyApp', 'Session'");
    }

    public override void Down()
    {
        throw new NotImplementedException("Down migrations not supported for sp_releaseapplock");
    }
}
```

#### External Distributed Lock (Redis Example)

```csharp
async Task RunMigrationsWithDistributedLock(IMigrationRunner runner)
{
    var resource = "my-app-migrations";
    var expiry = TimeSpan.FromMinutes(5);

    using var redLock = await redlockFactory.CreateLockAsync(resource, expiry);
    
    if (redLock.IsAcquired)
    {
        runner.MigrateUp();
    }
    else
    {
        throw new InvalidOperationException("Could not acquire migration lock");
    }
}
```

## Database-Specific Issues

### SQL Server Certificate Errors

**Error**: `The certificate chain was issued by an authority that is not trusted`

Since `Microsoft.Data.SqlClient` 4.0.0, connections are encrypted by default.

**Solutions:**

1. **Disable encryption** (for development):
```
Server=.;Database=MyDb;Integrated Security=true;Encrypt=False
```

2. **Trust server certificate**:
```
Server=.;Database=MyDb;Integrated Security=true;TrustServerCertificate=True
```

3. **Fix certificate** (recommended for production):
   - Install proper SSL certificate on SQL Server

### Oracle Stored Procedure Execution

**Error**: `ORA-00900: Invalid SQL Statement`

**Problem**: Oracle requires stored procedures to be wrapped in PL/SQL blocks.

**Solution:**
```csharp
// ❌ Won't work
Execute.Sql("DBMS_UTILITY.EXEC_DDL_STATEMENT('Create Index Member_AddrId On Member(AddrId)');");

// ✅ Correct approach
Execute.Sql(@"
BEGIN
    DBMS_UTILITY.EXEC_DDL_STATEMENT('Create Index Member_AddrId On Member(AddrId)');
END;");
```

### How do I get the SQL Server database name?

**Use case**: Performing `ALTER DATABASE` operations.

::: warning Important
To ALTER DATABASE, you must switch to `master` database first, then switch back to avoid running subsequent migrations in the wrong database.
:::

```csharp
public override void Up()
{
    Execute.Sql(@"
        DECLARE @DbName sysname = DB_NAME();
        DECLARE @SqlCommand NVARCHAR(MAX) = '
USE [master];
SET DEADLOCK_PRIORITY 10;

-- Your ALTER DATABASE commands here
ALTER DATABASE [' + @DbName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Maintenance operations here

ALTER DATABASE [' + @DbName + '] SET MULTI_USER;
';
        
        EXEC(@SqlCommand);
        
        -- Switch back to original database
        SET @SqlCommand = 'USE [' + @DbName + ']';
        EXEC(@SqlCommand);
    ");
}
```

## SQLite-Specific Issues

### Connection Pooling and File Locks

**Issue**: SQLite keeps database file locked after migration due to connection pooling.

**Problem**: Cannot delete or move database file after migration.

**Solution**: Disable connection pooling:
```
Data Source=mydb.db;Pooling=False;
```

With pooling disabled, you can safely delete or move the database file after the FluentMigrator processor is disposed.

## Performance and Optimization

### Large Dataset Migrations

**Issue**: Migration times out on large datasets.

**Solutions:**

1. **Increase timeout**:
```csharp
.ConfigureGlobalProcessorOptions(opt => {
    opt.Timeout = TimeSpan.FromMinutes(30);
})
```

2. **Batch operations**:
```csharp
public override void Up()
{
    // Process in batches to avoid memory issues
    var batchSize = 1000;
    var processed = 0;
    
    do
    {
        var sql = $@"
            UPDATE Users 
            SET ProcessedFlag = 1 
            WHERE ProcessedFlag = 0 
            AND Id IN (
                SELECT TOP {batchSize} Id 
                FROM Users 
                WHERE ProcessedFlag = 0 
                ORDER BY Id
            )";
            
        var rowsAffected = Execute.Sql(sql);
        processed += rowsAffected;
        
    } while (processed > 0);
}
```

### Index Creation on Large Tables

**Best practice**: Create indexes `ONLINE` when possible (SQL Server):

```csharp
public override void Up()
{
    // For large tables, create indexes online to avoid blocking
    IfDatabase("SqlServer")
        .Execute.Sql("CREATE INDEX IX_Users_Email ON Users(Email) WITH (ONLINE=ON)");
        
    // Fallback for other databases
    IfDatabase("Postgres", "MySql")
        .Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email");
}
```

## Development and Testing

### In-Memory Testing

**Use case**: Unit testing migrations without a real database.

```csharp
[Test]
public void TestMigration()
{
    var serviceProvider = new ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(rb => rb
            .AddSQLite()
            .WithGlobalConnectionString("Data Source=:memory:")
            .ScanIn(typeof(AddUserTable).Assembly).For.Migrations())
        .AddLogging(lb => lb.AddFluentMigratorConsole())
        .BuildServiceProvider(false);

    using var scope = serviceProvider.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    
    runner.MigrateUp();
    
    // Assert migration results
    Assert.That(runner.HasMigrationsToApplyUp(), Is.False);
}
```

### Migration Rollback Testing

**Best practice**: Always test Down() methods:

```csharp
[Test]
public void TestMigrationRollback()
{
    // Migrate up
    runner.MigrateUp(20231201120000);
    
    // Verify up migration
    Assert.That(Schema.Table("Users").Exists(), Is.True);
    
    // Migrate down
    runner.MigrateDown(20231201120000);
    
    // Verify down migration
    Assert.That(Schema.Table("Users").Exists(), Is.False);
}
```

## Troubleshooting Tools

### Enable Verbose Logging

```csharp
.AddLogging(lb => lb
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug))
```

### Preview Migrations Without Execution

```bash
# Console runner
Migrate.exe -p sqlserver -c "..." -a "MyApp.dll" --preview

# dotnet-fm
dotnet fm migrate -p sqlserver -c "..." -a "MyApp.dll" --preview
```

### Generate SQL Scripts

```bash
# Output SQL to file without executing
dotnet fm migrate -p sqlserver -c "..." -a "MyApp.dll" --output --outputFileName "migration.sql"
```

### Validate Migrations

```bash
# Validate without connecting to database
dotnet fm validate -p sqlserver -c "..." -a "MyApp.dll" --noConnection
```

## Common Error Messages

### "Migration XYZ has already been applied"
This usually indicates:
- Migration version conflicts
- Version table corruption
- Manual database changes

**Solution**: Check the VersionInfo table and resolve conflicts.

### "Syntax error near 'GO'"
FluentMigrator doesn't support SQL batch separators like `GO`.

**Solution**: Split statements or use `Execute.Sql()` for each batch.

### "Object name 'dbo.VersionInfo' is invalid"
The version tracking table hasn't been created.

**Solution**: Ensure the database user has CREATE TABLE permissions.

## Getting Help

If you encounter issues not covered in this FAQ:

1. **Search existing issues**: [GitHub Issues](https://github.com/fluentmigrator/fluentmigrator/issues)
2. **Check discussions**: [GitHub Discussions](https://github.com/fluentmigrator/fluentmigrator/discussions)
3. **Ask on Stack Overflow**: Use the `fluentmigrator` tag
4. **Create a new issue**: Include:
   - FluentMigrator version
   - Database provider and version
   - Complete error message and stack trace
   - Minimal reproducible example