# Best Practices

This comprehensive guide covers best practices for using FluentMigrator effectively, including database design, migration patterns, team collaboration, testing strategies, and production deployment considerations.

## Migration Design Principles

### 1. Keep Migrations Small and Focused

```csharp
// ✅ Good: Single focused migration
[Migration(202401011200)]
public class AddEmailToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).Nullable();
    }

    public override void Down()
    {
        Delete.Column("Email").FromTable("Users");
    }
}

// ❌ Bad: Multiple unrelated changes in one migration
[Migration(202401011300)]
public class MassiveChanges : Migration
{
    public override void Up()
    {
        // Adding email column
        Alter.Table("Users").AddColumn("Email").AsString(255).Nullable();

        // Creating completely unrelated table
        Create.Table("Products")...

        // Modifying another unrelated table
        Alter.Table("Orders")...
    }
    // This makes it hard to rollback specific changes
}
```

### 2. Always Provide Rollback Logic

```csharp
[Migration(202401011400)]
public class AddUserStatusColumn : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active");

        // Update existing users to have a status
        Execute.Sql("UPDATE Users SET Status = 'Active' WHERE Status IS NULL");
    }

    public override void Down()
    {
        // Always provide meaningful rollback
        Delete.Column("Status").FromTable("Users");
    }
}
```

### 3. Use Descriptive Migration Names and Version Numbers

```csharp
// ✅ Good: Descriptive names with meaningful timestamps
[Migration(202401151430)] // YYYYMMDDHHNN format
public class AddUserEmailVerificationColumns : Migration { }

[Migration(202401151435)]
public class CreateProductCatalogTables : Migration { }

[Migration(202401151440)]
public class AddIndexesToUserTable : Migration { }

// ❌ Bad: Generic names and random numbers
[Migration(1)]
public class Migration1 : Migration { }

[Migration(12345)]
public class UpdateStuff : Migration { }
```

## Data Migration Best Practices

### 1. Safe Data Migrations

```csharp
[Migration(202401151500)]
public class MigrateUserFullNames : Migration
{
    public override void Up()
    {
        // Step 1: Add new column as nullable first
        Alter.Table("Users")
            .AddColumn("FullName").AsString(200).Nullable();

        // Step 2: Populate the new column with data
        Execute.Sql(@"
            UPDATE Users
            SET FullName = COALESCE(FirstName + ' ' + LastName, FirstName, LastName, 'Unknown')
            WHERE FullName IS NULL");

        // Step 3: Verify data migration was successful
        var nullCount = Execute.Sql("SELECT COUNT(*) FROM Users WHERE FullName IS NULL")
            .Returns<int>().FirstOrDefault();

        if (nullCount > 0)
        {
            throw new InvalidOperationException($"Data migration failed: {nullCount} users still have null FullName");
        }

        // Step 4: Make column not nullable after data is migrated
        Alter.Column("FullName").OnTable("Users")
            .AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("FullName").FromTable("Users");
    }
}
```

### 2. Handling Large Dataset Migrations

```csharp
[Migration(202401151600)]
public class MigrateLargeDataset : Migration
{
    public override void Up()
    {
        // For very large tables, process in batches
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                DECLARE @BatchSize INT = 10000;
                DECLARE @RowsUpdated INT = @BatchSize;

                WHILE @RowsUpdated = @BatchSize
                BEGIN
                    UPDATE TOP (@BatchSize) Users
                    SET LastModified = GETDATE()
                    WHERE LastModified IS NULL;

                    SET @RowsUpdated = @@ROWCOUNT;

                    -- Brief pause to avoid overwhelming the database
                    WAITFOR DELAY '00:00:01';
                END");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                DO $$
                DECLARE
                    batch_size INTEGER := 10000;
                    rows_updated INTEGER;
                BEGIN
                    LOOP
                        UPDATE Users
                        SET last_modified = NOW()
                        WHERE id IN (
                            SELECT id FROM Users
                            WHERE last_modified IS NULL
                            LIMIT batch_size
                        );

                        GET DIAGNOSTICS rows_updated = ROW_COUNT;
                        EXIT WHEN rows_updated = 0;

                        -- Brief pause
                        PERFORM pg_sleep(1);
                    END LOOP;
                END $$");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET LastModified = NULL");
    }
}
```

### 3. Validation and Data Integrity Checks

```csharp
[Migration(202401151700)]
public class AddEmailWithValidation : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).Nullable();

        // Add check constraint for email format
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                ALTER TABLE Users
                ADD CONSTRAINT CK_Users_Email_Format
                CHECK (Email IS NULL OR Email LIKE '%_@_%.__%')");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                ALTER TABLE Users
                ADD CONSTRAINT CK_Users_Email_Format
                CHECK (Email IS NULL OR Email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')");

        // Create unique index for non-null emails
        Execute.Sql(@"
            CREATE UNIQUE INDEX UQ_Users_Email
            ON Users (Email)
            WHERE Email IS NOT NULL");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS UQ_Users_Email");

            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("ALTER TABLE Users DROP CONSTRAINT CK_Users_Email_Format");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql("ALTER TABLE Users DROP CONSTRAINT CK_Users_Email_Format");

        Delete.Column("Email").FromTable("Users");
    }
}
```

## Performance Best Practices

### 1. Index Strategy

```csharp
[Migration(202401151800)]
public class OptimizeUserTableIndexes : Migration
{
    public override void Up()
    {
        Create.Table("OptimizedUsers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("LastLoginAt").AsDateTime().Nullable()
            .WithColumn("CompanyId").AsInt32().NotNullable();

        // 1. Unique indexes for business keys
        Create.Index("UQ_OptimizedUsers_Username")
            .OnTable("OptimizedUsers")
            .OnColumn("Username")
            .Unique();

        Create.Index("UQ_OptimizedUsers_Email")
            .OnTable("OptimizedUsers")
            .OnColumn("Email")
            .Unique();

        // 2. Foreign key indexes for join performance
        Create.Index("IX_OptimizedUsers_CompanyId")
            .OnTable("OptimizedUsers")
            .OnColumn("CompanyId");

        // 3. Composite indexes for common query patterns
        Create.Index("IX_OptimizedUsers_Company_Status_Created")
            .OnTable("OptimizedUsers")
            .OnColumn("CompanyId")    // Most selective first
            .OnColumn("Status")
            .OnColumn("CreatedAt");   // Used for sorting

        // 4. Covering indexes for read-heavy queries
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Create.Index("IX_OptimizedUsers_Status_Covering")
                .OnTable("OptimizedUsers")
                .OnColumn("Status")
                .WithOptions()
                .Include("Username")
                .Include("Email")
                .Include("CreatedAt");
    });

        // 5. Filtered indexes for common subsets
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                CREATE INDEX IX_OptimizedUsers_ActiveUsers
                ON OptimizedUsers (CreatedAt DESC)
                WHERE Status = 'Active'");
    IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                CREATE INDEX IX_OptimizedUsers_ActiveUsers
                ON OptimizedUsers (CreatedAt DESC)
                WHERE Status = 'Active'");
    }

    public override void Down()
    {
        Delete.Table("OptimizedUsers");
    }
}
```

### 2. Query Performance Optimization

```csharp
[Migration(202401151900)]
public class CreatePerformanceOptimizedViews : Migration
{
    public override void Up()
    {
        // Create materialized aggregations for expensive queries
        Create.Table("UserStatsSummary")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CompanyId").AsInt32().NotNullable()
            .WithColumn("ActiveUserCount").AsInt32().NotNullable()
            .WithColumn("TotalUserCount").AsInt32().NotNullable()
            .WithColumn("LastUpdated").AsDateTime().NotNullable();

        // Populate initial data
        Execute.Sql(@"
            INSERT INTO UserStatsSummary (CompanyId, ActiveUserCount, TotalUserCount, LastUpdated)
            SELECT
                CompanyId,
                SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) as ActiveUserCount,
                COUNT(*) as TotalUserCount,
                GETDATE() as LastUpdated
            FROM Users
            GROUP BY CompanyId");

        // Create index for fast lookups
        Create.Index("IX_UserStatsSummary_CompanyId")
            .OnTable("UserStatsSummary")
            .OnColumn("CompanyId")
            .Unique();

        // Create view for easy access
        Execute.Sql(@"
            CREATE VIEW UserStatsView AS
            SELECT
                u.CompanyId,
                c.CompanyName,
                u.ActiveUserCount,
                u.TotalUserCount,
                CAST(u.ActiveUserCount AS FLOAT) / u.TotalUserCount * 100 as ActivePercentage,
                u.LastUpdated
            FROM UserStatsSummary u
            LEFT JOIN Companies c ON u.CompanyId = c.Id");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW UserStatsView");
        Delete.Table("UserStatsSummary");
    }
}
```

## Team Collaboration Best Practices

### 1. Migration Naming and Organization

```csharp
// Use consistent timestamp format: YYYYMMDDHHNN
// Group related migrations by feature/story

// Feature: User Profile Enhancement
[Migration(202401201400)]
public class AddUserProfileFields : Migration { }

[Migration(202401201405)]
public class AddUserPreferencesTable : Migration { }

[Migration(202401201410)]
public class AddUserProfileIndexes : Migration { }

// Feature: Order Management System
[Migration(202401201500)]
public class CreateOrderTables : Migration { }

[Migration(202401201505)]
public class AddOrderStatusTracking : Migration { }

[Migration(202401201510)]
public class AddOrderIndexesAndConstraints : Migration { }
```

### 2. Documentation in Migrations

```csharp
[Migration(202401201600)]
public class AddUserTierSystemForBillingRestructure : Migration
{
    /// <summary>
    /// Story: US-1234 - Implement user tier system for new billing structure
    ///
    /// This migration adds support for user tiers (Basic, Premium, Enterprise)
    /// to support the new billing system launching in Q2.
    ///
    /// Breaking Changes: None - all existing users will default to 'Basic' tier
    /// Data Migration: All existing users will be set to 'Basic' tier
    /// </summary>
    public override void Up()
    {
        // Add user tier support
        Alter.Table("Users")
            .AddColumn("UserTier").AsString(20).NotNullable().WithDefaultValue("Basic");

        // Add constraint to ensure valid tier values
        Execute.Sql(@"
            ALTER TABLE Users
            ADD CONSTRAINT CK_Users_UserTier
            CHECK (UserTier IN ('Basic', 'Premium', 'Enterprise'))");

        // Create billing-related table for tier pricing
        Create.Table("UserTierPricing")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TierName").AsString(20).NotNullable()
            .WithColumn("MonthlyPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("AnnualPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("MaxUsers").AsInt32().Nullable() // NULL = unlimited
            .WithColumn("EffectiveDate").AsDateTime().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Insert initial pricing data
        Insert.IntoTable("UserTierPricing")
            .Row(new { TierName = "Basic", MonthlyPrice = 0.00m, AnnualPrice = 0.00m, MaxUsers = 5, EffectiveDate = DateTime.Now, IsActive = true })
            .Row(new { TierName = "Premium", MonthlyPrice = 29.99m, AnnualPrice = 299.99m, MaxUsers = 50, EffectiveDate = DateTime.Now, IsActive = true })
            .Row(new { TierName = "Enterprise", MonthlyPrice = 99.99m, AnnualPrice = 999.99m, MaxUsers = (int?)null, EffectiveDate = DateTime.Now, IsActive = true });
    }

    public override void Down()
    {
        Delete.Table("UserTierPricing");

        Execute.Sql("ALTER TABLE Users DROP CONSTRAINT CK_Users_UserTier");
        Delete.Column("UserTier").FromTable("Users");
    }
}
```

### 3. Environment-Specific Migrations

```csharp
[Migration(202401201700)]
public class AddDevelopmentTestData : Migration
{
    public override void Up()
    {
        // Only add test data in development environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            Insert.IntoTable("Users")
                .Row(new { Username = "developer", Email = "dev@company.com", Status = "Active", CreatedAt = DateTime.Now })
                .Row(new { Username = "tester", Email = "test@company.com", Status = "Active", CreatedAt = DateTime.Now })
                .Row(new { Username = "admin", Email = "admin@company.com", Status = "Active", CreatedAt = DateTime.Now });
        }

        // Production-safe changes that apply to all environments
        Create.Index("IX_Users_Status_CreatedAt")
            .OnTable("Users")
            .OnColumn("Status")
            .OnColumn("CreatedAt");
    }

    public override void Down()
    {
        Delete.Index("IX_Users_Status_CreatedAt").OnTable("Users");

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            Delete.FromTable("Users").Row(new { Username = "developer" });
            Delete.FromTable("Users").Row(new { Username = "tester" });
            Delete.FromTable("Users").Row(new { Username = "admin" });
        }
    }
}
```

## Testing Strategies

### 1. Migration Testing Patterns

```csharp
// Create a base test class for migration testing
public abstract class MigrationTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IMigrationRunner Runner { get; private set; }

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString("Data Source=:memory:")
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        ServiceProvider = services.BuildServiceProvider(false);
        Runner = ServiceProvider.GetRequiredService<IMigrationRunner>();
    }

    [TearDown]
    public void TearDown()
    {
        ServiceProvider?.Dispose();
    }
}

// Example migration test
[TestFixture]
public class AddUserEmailMigrationTests : MigrationTestBase
{
    [Test]
    public void Migration_ShouldAddEmailColumn_WhenApplied()
    {
        // Arrange
        Runner.MigrateUp(202401201000); // Run migrations up to before our target migration

        // Act
        Runner.MigrateUp(202401201100); // Run our specific migration

        // Assert
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var columnExists = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM pragma_table_info('Users')
            WHERE name = 'Email'") > 0;

        Assert.IsTrue(columnExists, "Email column should exist after migration");
    }

    [Test]
    public void Migration_ShouldRemoveEmailColumn_WhenRolledBack()
    {
        // Arrange
        Runner.MigrateUp(202401201100);

        // Act
        Runner.MigrateDown(202401201000);

        // Assert
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var columnExists = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM pragma_table_info('Users')
            WHERE name = 'Email'") > 0;

        Assert.IsFalse(columnExists, "Email column should not exist after rollback");
    }

    [Test]
    public void Migration_ShouldPreserveExistingData_WhenApplied()
    {
        // Arrange
        Runner.MigrateUp(202401201000);

        // Insert test data
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("INSERT INTO Users (Username, Status, CreatedAt) VALUES ('testuser', 'Active', datetime('now'))");

        var originalCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");

        // Act
        Runner.MigrateUp(202401201100);

        // Assert
        var newCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
        Assert.AreEqual(originalCount, newCount, "Migration should preserve existing data");

        // Verify new column is present and nullable
        var emailValue = connection.ExecuteScalar("SELECT Email FROM Users WHERE Username = 'testuser'");
        Assert.IsNull(emailValue, "New Email column should be nullable for existing records");
    }
}
```

### 2. Integration Testing

```csharp
[TestFixture]
public class DatabaseMigrationIntegrationTests
{
    private string _connectionString;
    private IServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Use a test database for integration tests
        _connectionString = "Data Source=test_migration_db.sqlite";

        _serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
        if (File.Exists("test_migration_db.sqlite"))
            File.Delete("test_migration_db.sqlite");
    }

    [Test]
    public void FullMigrationCycle_ShouldCompleteSuccessfully()
    {
        // Arrange
        var runner = _serviceProvider.GetRequiredService<IMigrationRunner>();

        // Act & Assert - Run all migrations up
        Assert.DoesNotThrow(() => runner.MigrateUp());

        // Verify final database state
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var tables = connection.Query<string>(@"
            SELECT name FROM sqlite_master
            WHERE type='table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name");

        var expectedTables = new[] { "Users", "Companies", "Orders", "VersionInfo" };
        CollectionAssert.IsSubsetOf(expectedTables, tables.ToList());

        // Test rollback of last few migrations
        Assert.DoesNotThrow(() => runner.Rollback(3));

        // Test re-applying migrations
        Assert.DoesNotThrow(() => runner.MigrateUp());
    }

    [Test]
    public void Migration_ShouldHandleConcurrentExecution()
    {
        // Test that migration runner handles concurrent execution properly
        var runner1 = _serviceProvider.GetRequiredService<IMigrationRunner>();
        var runner2 = _serviceProvider.GetRequiredService<IMigrationRunner>();

        var task1 = Task.Run(() => runner1.MigrateUp());
        var task2 = Task.Run(() => runner2.MigrateUp());

        Assert.DoesNotThrow(() => Task.WaitAll(task1, task2));
    }
}
```

## Production Deployment Best Practices

### 1. Pre-Deployment Validation

```csharp
[Migration(202401201800)]
public class ProductionReadyMigration : Migration
{
    public override void Up()
    {
        // 1. Check for potential blocking operations
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProduction = environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;

        if (isProduction)
        {
            // In production, avoid long-running operations during business hours
            var currentHour = DateTime.Now.Hour;
            if (currentHour >= 9 && currentHour <= 17) // 9 AM to 5 PM
            {
                Console.WriteLine("WARNING: Running migration during business hours. Consider scheduling during maintenance window.");
            }
        }

        // 2. Estimate migration duration for large tables
        var userCount = Execute.Sql("SELECT COUNT(*) FROM Users").Returns<int>().FirstOrDefault();
        if (userCount > 1000000) // 1 million records
        {
            Console.WriteLine($"WARNING: Large table migration ({userCount:N0} records). Estimated time: {EstimateMigrationTime(userCount)} minutes");
        }

        // 3. Create new column as nullable first (non-blocking)
        Alter.Table("Users")
            .AddColumn("Department").AsString(100).Nullable();

        // 4. Add default value for new records
        Execute.Sql("UPDATE Users SET Department = 'General' WHERE Department IS NULL");

        // 5. Make column not nullable (potentially blocking - do this during maintenance)
        if (!isProduction || IsMaintenanceWindow())
        {
            Alter.Column("Department").OnTable("Users")
                .AsString(100).NotNullable().WithDefaultValue("General");
        }
        else
        {
            Console.WriteLine("NOTICE: Skipping NOT NULL constraint in production during business hours. Run during maintenance window.");
        }
    }

    private bool IsMaintenanceWindow()
    {
        var currentHour = DateTime.Now.Hour;
        return currentHour < 6 || currentHour > 22; // Before 6 AM or after 10 PM
    }

    private int EstimateMigrationTime(int recordCount)
    {
        // Rough estimate: 1000 records per second
        return (recordCount / 1000) / 60;
    }

    public override void Down()
    {
        Delete.Column("Department").FromTable("Users");
    }
}
```

### 2. Zero-Downtime Migration Patterns

```csharp
// Phase 1: Add new column/table without breaking existing code
[Migration(202401201900)]
public class AddUserLocationPhase1 : Migration
{
    public override void Up()
    {
        // Add new columns as nullable - won't break existing code
        Alter.Table("Users")
            .AddColumn("Country").AsString(50).Nullable()
            .AddColumn("Region").AsString(50).Nullable()
            .AddColumn("City").AsString(100).Nullable();

        // Add indexes for new columns
        Create.Index("IX_Users_Country").OnTable("Users").OnColumn("Country");
        Create.Index("IX_Users_Region").OnTable("Users").OnColumn("Region");
    }

    public override void Down()
    {
        Delete.Index("IX_Users_Region").OnTable("Users");
        Delete.Index("IX_Users_Country").OnTable("Users");
        Delete.Column("City").FromTable("Users");
        Delete.Column("Region").FromTable("Users");
        Delete.Column("Country").FromTable("Users");
    }
}

// Phase 2: Populate new columns (deploy new application code that uses both old and new)
[Migration(202401201905)]
public class AddUserLocationPhase2 : Migration
{
    public override void Up()
    {
        // Migrate data from old address field to new structured fields
        Execute.Sql(@"
            UPDATE Users
            SET
                Country = 'USA',  -- Default for existing data
                Region = CASE
                    WHEN Address LIKE '%CA%' THEN 'California'
                    WHEN Address LIKE '%NY%' THEN 'New York'
                    ELSE 'Unknown'
                END
            WHERE Country IS NULL");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET Country = NULL, Region = NULL, City = NULL");
    }
}

// Phase 3: Make columns required after application code is updated
[Migration(202401201910)]
public class AddUserLocationPhase3 : Migration
{
    public override void Up()
    {
        // Now that application is updated, make columns required
        Alter.Column("Country").OnTable("Users")
            .AsString(50).NotNullable().WithDefaultValue("USA");

        Alter.Column("Region").OnTable("Users")
            .AsString(50).NotNullable().WithDefaultValue("Unknown");
    }

    public override void Down()
    {
        Alter.Column("Region").OnTable("Users").AsString(50).Nullable();
        Alter.Column("Country").OnTable("Users").AsString(50).Nullable();
    }
}

// Phase 4: Clean up old columns (after confirming new system works)
[Migration(202401201915)]
public class AddUserLocationPhase4 : Migration
{
    public override void Up()
    {
        // Remove old address field after confirming new location fields work
        Delete.Column("Address").FromTable("Users");
    }

    public override void Down()
    {
        Alter.Table("Users")
            .AddColumn("Address").AsString(500).Nullable();
    }
}
```

### 3. Backup and Recovery Strategies

```csharp
[Migration(202401202000)]
public class ProductionSafeMigrationWithBackup : Migration
{
    public override void Up()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProduction = environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;

        if (isProduction)
        {
            // 1. Verify backup exists before proceeding
            Console.WriteLine("PRODUCTION MIGRATION: Verifying recent backup exists...");

            if (!VerifyRecentBackupExists())
            {
                throw new InvalidOperationException("No recent backup found. Please create backup before running migration.");
            }

            // 2. Create additional safety backup of affected tables
            Console.WriteLine("Creating safety backup of Users table...");
            Execute.Sql($"SELECT * INTO Users_Backup_{DateTime.Now:yyyyMMddHHmm} FROM Users");
        }

        try
        {
            // Perform the actual migration
            Alter.Table("Users")
                .AddColumn("SecurityLevel").AsInt32().NotNullable().WithDefaultValue(1);

            // Validate the migration was successful
            var newColumnExists = Execute.Sql(@"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'SecurityLevel'")
                .Returns<int>().FirstOrDefault();

            if (newColumnExists == 0)
            {
                throw new InvalidOperationException("Migration failed: SecurityLevel column was not created");
            }

            Console.WriteLine("Migration completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");

            if (isProduction)
            {
                Console.WriteLine("PRODUCTION ERROR: Consider restoring from backup if needed");
            }

            throw;
        }
    }

    private bool VerifyRecentBackupExists()
    {
        // This would be specific to your backup system
        // Example: check if backup file newer than 24 hours exists
        try
        {
            var backupDirectory = @"C:\DatabaseBackups";
            if (!Directory.Exists(backupDirectory))
                return false;

            var recentBackups = Directory.GetFiles(backupDirectory, "*.bak")
                .Where(f => File.GetCreationTime(f) > DateTime.Now.AddHours(-24))
                .ToList();

            return recentBackups.Any();
        }
        catch
        {
            return false; // Assume no backup if we can't check
        }
    }

    public override void Down()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProduction = environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;

        if (isProduction)
        {
            Console.WriteLine("PRODUCTION ROLLBACK: Proceeding with column removal");
        }

        Delete.Column("SecurityLevel").FromTable("Users");

        Console.WriteLine("Rollback completed successfully");
    }
}
```

## Monitoring and Maintenance

### 1. Migration Logging and Monitoring

```csharp
[Migration(202401202100)]
public class MonitoredMigration : Migration
{
    public override void Up()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var migrationId = GetType().GetCustomAttribute<MigrationAttribute>()?.Version ?? 0;

        try
        {
            Console.WriteLine($"Starting migration {migrationId} at {DateTime.Now}");

            // Log to application monitoring system
            LogMigrationStart(migrationId);

            // Perform migration
            Create.Table("UserSessions")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("SessionStart").AsDateTime().NotNullable()
                .WithColumn("SessionEnd").AsDateTime().Nullable()
                .WithColumn("IPAddress").AsString(45).Nullable()
                .WithColumn("UserAgent").AsString(500).Nullable();

            Create.Index("IX_UserSessions_UserId").OnTable("UserSessions").OnColumn("UserId");
            Create.Index("IX_UserSessions_SessionStart").OnTable("UserSessions").OnColumn("SessionStart");

            stopwatch.Stop();

            Console.WriteLine($"Migration {migrationId} completed successfully in {stopwatch.ElapsedMilliseconds}ms");
            LogMigrationSuccess(migrationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"Migration {migrationId} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            LogMigrationFailure(migrationId, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    private void LogMigrationStart(long migrationId)
    {
        // Log to your monitoring system (e.g., Application Insights, DataDog, etc.)
        // Example:
        // _telemetryClient.TrackEvent("MigrationStarted", new Dictionary<string, string>
        // {
        //     ["MigrationId"] = migrationId.ToString(),
        //     ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        // });
    }

    private void LogMigrationSuccess(long migrationId, long durationMs)
    {
        // Log success metrics
        // _telemetryClient.TrackEvent("MigrationCompleted", new Dictionary<string, string>
        // {
        //     ["MigrationId"] = migrationId.ToString(),
        //     ["Duration"] = durationMs.ToString()
        // });
    }

    private void LogMigrationFailure(long migrationId, long durationMs, Exception ex)
    {
        // Log failure with exception details
        // _telemetryClient.TrackException(ex, new Dictionary<string, string>
        // {
        //     ["MigrationId"] = migrationId.ToString(),
        //     ["Duration"] = durationMs.ToString()
        // });
    }

    public override void Down()
    {
        Console.WriteLine($"Rolling back migration {GetType().GetCustomAttribute<MigrationAttribute>()?.Version ?? 0}");
        Delete.Table("UserSessions");
    }
}
```

### 2. Health Checks and Validation

```csharp
[Migration(202401202200)]
public class MigrationWithHealthChecks : Migration
{
    public override void Up()
    {
        // Pre-migration health checks
        ValidateDatabaseState();

        // Perform migration
        Alter.Table("Users")
            .AddColumn("TwoFactorEnabled").AsBoolean().NotNullable().WithDefaultValue(false);

        // Post-migration validation
        ValidateMigrationResults();

        Console.WriteLine("Migration and validation completed successfully");
    }

    private void ValidateDatabaseState()
    {
        // Check database connectivity
        var canConnect = Execute.Sql("SELECT 1").Returns<int>().FirstOrDefault() == 1;
        if (!canConnect)
            throw new InvalidOperationException("Database connectivity check failed");

        // Check table exists
        var usersTableExists = Schema.Table("Users").Exists();
        if (!usersTableExists)
            throw new InvalidOperationException("Users table does not exist");

        // Check for sufficient disk space (example - would be database-specific)
        var freeSpaceCheck = Execute.Sql(@"
            SELECT CASE
                WHEN size > 1000000 THEN 1  -- If DB is > 1GB, assume we have space checks
                ELSE 1
            END
            FROM sys.database_files
            WHERE type = 0") // SQL Server specific
            .Returns<int>().FirstOrDefault();

        Console.WriteLine("Pre-migration validation passed");
    }

    private void ValidateMigrationResults()
    {
        // Verify column was created
        var columnExists = IfDatabase(ProcessorIdConstants.SqlServer) ?
            Execute.Sql(@"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'TwoFactorEnabled'")
                .Returns<int>().FirstOrDefault() > 0 :
            Schema.Table("Users").Column("TwoFactorEnabled").Exists();

        if (!columnExists)
            throw new InvalidOperationException("TwoFactorEnabled column was not created successfully");

        // Verify default value works
        var defaultValue = Execute.Sql(@"
            SELECT TwoFactorEnabled
            FROM Users
            WHERE TwoFactorEnabled IS NOT NULL")
            .Returns<bool?>().FirstOrDefault();

        if (defaultValue == null)
            throw new InvalidOperationException("Default value not applied correctly");

        // Check record count hasn't changed unexpectedly
        var recordCount = Execute.Sql("SELECT COUNT(*) FROM Users").Returns<int>().FirstOrDefault();
        if (recordCount == 0)
            Console.WriteLine("WARNING: Users table is empty");

        Console.WriteLine("Post-migration validation passed");
    }

    public override void Down()
    {
        Delete.Column("TwoFactorEnabled").FromTable("Users");

        // Post-rollback validation
        var columnExists = Schema.Table("Users").Column("TwoFactorEnabled").Exists();
        if (columnExists)
            throw new InvalidOperationException("Rollback failed: TwoFactorEnabled column still exists");

        Console.WriteLine("Rollback validation passed");
    }
}
```

## Common Anti-Patterns to Avoid

### ❌ Bad Practices

```csharp
// DON'T: Massive migrations that do too much
[Migration(202401210001)]
public class MassiveRefactorMigration : Migration
{
    public override void Up()
    {
        // This does too much in one migration:
        // 1. Restructures multiple tables
        // 2. Migrates large amounts of data
        // 3. Changes business logic
        // 4. Has complex rollback scenarios

        // Makes it impossible to rollback partially
        // Hard to troubleshoot if something goes wrong
        // Long-running migration blocks other operations
    }
}

// DON'T: Ignore rollback scenarios
[Migration(202401210002)]
public class NoRollbackMigration : Migration
{
    public override void Up()
    {
        Create.Table("NewTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("Data").AsString(500).NotNullable();

        // Complex data migration that can't be reversed
        Execute.Sql("Complex data transformation that destroys original data");
    }

    public override void Down()
    {
        // Empty or throws exception - bad practice!
        throw new NotSupportedException("This migration cannot be rolled back");
    }
}

// DON'T: Hard-code environment-specific values
[Migration(202401210003)]
public class HardcodedValuesMigration : Migration
{
    public override void Up()
    {
        Insert.IntoTable("Settings")
            .Row(new { Key = "ApiUrl", Value = "https://prod-api.company.com" }); // Wrong!

        Execute.Sql(@"
            UPDATE Users
            SET Company = 'Production Company Inc'
            WHERE Company IS NULL"); // Wrong!
    }
}
```

### ✅ Better Approaches

```csharp
// DO: Break large changes into smaller migrations
[Migration(202401210101)]
public class AddUserMetadataTablePhase1 : Migration
{
    public override void Up()
    {
        Create.Table("UserMetadata")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("Key").AsString(100).NotNullable()
            .WithColumn("Value").AsString(1000).Nullable();
    }

    public override void Down()
    {
        Delete.Table("UserMetadata");
    }
}

[Migration(202401210102)]
public class AddUserMetadataIndexesPhase2 : Migration
{
    public override void Up()
    {
        Create.Index("IX_UserMetadata_UserId").OnTable("UserMetadata").OnColumn("UserId");
        Create.Index("IX_UserMetadata_Key").OnTable("UserMetadata").OnColumn("Key");
        Create.Index("UQ_UserMetadata_UserId_Key").OnTable("UserMetadata")
            .OnColumn("UserId").OnColumn("Key").Unique();
    }

    public override void Down()
    {
        Delete.Index("UQ_UserMetadata_UserId_Key").OnTable("UserMetadata");
        Delete.Index("IX_UserMetadata_Key").OnTable("UserMetadata");
        Delete.Index("IX_UserMetadata_UserId").OnTable("UserMetadata");
    }
}

// DO: Always provide meaningful rollback
[Migration(202401210103)]
public class ReversibleDataMigration : Migration
{
    public override void Up()
    {
        // Store original values before changing them
        Execute.Sql(@"
            CREATE TABLE #OriginalUserStatus AS
            SELECT Id, Status
            FROM Users
            WHERE Status = 'Inactive'");

        // Make the change
        Execute.Sql("UPDATE Users SET Status = 'Disabled' WHERE Status = 'Inactive'");
    }

    public override void Down()
    {
        // Restore original values
        Execute.Sql(@"
            UPDATE u
            SET Status = o.Status
            FROM Users u
            INNER JOIN #OriginalUserStatus o ON u.Id = o.Id");

        Execute.Sql("DROP TABLE #OriginalUserStatus");
    }
}

// DO: Use configuration for environment-specific values
[Migration(202401210104)]
public class ConfigurableValuesMigration : Migration
{
    public override void Up()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var defaultApiUrl = environment switch
        {
            "Development" => "https://dev-api.company.com",
            "Staging" => "https://staging-api.company.com",
            "Production" => "https://api.company.com",
            _ => "https://api.company.com"
        };

        Insert.IntoTable("Settings")
            .Row(new { Key = "ApiUrl", Value = defaultApiUrl });
    }

    public override void Down()
    {
        Delete.FromTable("Settings").Row(new { Key = "ApiUrl" });
    }
}
```

This comprehensive best practices guide provides the foundation for successful database migrations with FluentMigrator. Following these patterns will help ensure your migrations are reliable, maintainable, and safe for production deployment.
