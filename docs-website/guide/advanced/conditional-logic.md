# Conditional Logic in Migrations

FluentMigrator provides powerful conditional logic capabilities that allow you to create adaptive migrations that behave differently based on database providers, environments, existing schema state, or custom conditions. This guide covers all aspects of conditional migration logic.

## Database Provider Conditionals

### Basic Provider Detection

```csharp
[Migration(202401151000)]
public class DatabaseProviderConditionals : Migration
{
    public override void Up()
    {
        Create.Table("CrossPlatformTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // SQL Server specific features
        if (IfDatabase("SqlServer"))
        {
            // Add SQL Server specific columns
            Alter.Table("CrossPlatformTable")
                .AddColumn("RowVersion").AsCustom("ROWVERSION").NotNullable()
                .AddColumn("XmlData").AsCustom("XML").Nullable();
                
            // Create filtered index
            Execute.Sql(@"
                CREATE INDEX IX_CrossPlatformTable_Name_Active 
                ON CrossPlatformTable (Name) 
                WHERE Name IS NOT NULL");
        }
        else if (IfDatabase("Postgres"))
        {
            // PostgreSQL specific features
            Alter.Table("CrossPlatformTable")
                .AddColumn("JsonData").AsCustom("JSONB").Nullable()
                .AddColumn("ArrayData").AsCustom("TEXT[]").Nullable();
                
            // Create GIN index for JSONB
            Execute.Sql(@"
                CREATE INDEX IX_CrossPlatformTable_JsonData 
                ON CrossPlatformTable USING GIN (JsonData)");
        }
        else if (IfDatabase("MySQL"))
        {
            // MySQL specific features
            Alter.Table("CrossPlatformTable")
                .AddColumn("JsonData").AsCustom("JSON").Nullable()
                .AddColumn("Status").AsCustom("ENUM('Active','Inactive','Pending')").NotNullable().WithDefaultValue(RawSql.Insert("'Active'"));
                
            // Set storage engine
            Execute.Sql("ALTER TABLE CrossPlatformTable ENGINE=InnoDB");
        }
        else if (IfDatabase("SQLite"))
        {
            // SQLite specific handling
            Alter.Table("CrossPlatformTable")
                .AddColumn("JsonData").AsString(4000).Nullable(); // JSON as TEXT
                
            // SQLite doesn't support filtered WHERE clauses in older versions
            Execute.Sql("CREATE INDEX IX_CrossPlatformTable_Name ON CrossPlatformTable (Name)");
        }
        else if (IfDatabase("Oracle"))
        {
            // Oracle specific features
            Alter.Table("CrossPlatformTable")
                .AddColumn("XmlData").AsCustom("XMLType").Nullable()
                .AddColumn("LobData").AsCustom("CLOB").Nullable();
                
            // Create Oracle-style index
            Execute.Sql("CREATE INDEX IX_CrossPlatformTable_Name ON CrossPlatformTable (UPPER(Name))");
        }
    }

    public override void Down()
    {
        Delete.Table("CrossPlatformTable");
    }
}
```

### Advanced Provider-Specific Logic

```csharp
[Migration(202401151100)]
public class AdvancedProviderLogic : Migration
{
    public override void Up()
    {
        Create.Table("AdvancedFeatures")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Amount").AsDecimal(15, 2).NotNullable();
            
        // Database provider with version checking
        if (IfDatabase("SqlServer"))
        {
            var version = GetSqlServerVersion();
            if (version.Major >= 13) // SQL Server 2016+
            {
                // Use JSON support in SQL Server 2016+
                Alter.Table("AdvancedFeatures")
                    .AddColumn("JsonData").AsCustom("NVARCHAR(MAX)").Nullable();
                    
                Execute.Sql(@"
                    ALTER TABLE AdvancedFeatures 
                    ADD CONSTRAINT CK_AdvancedFeatures_JsonData 
                    CHECK (JsonData IS NULL OR ISJSON(JsonData) = 1)");
            }
            else
            {
                // Fallback for older SQL Server versions
                Alter.Table("AdvancedFeatures")
                    .AddColumn("JsonData").AsString(4000).Nullable();
            }
            
            if (version.Major >= 14) // SQL Server 2017+
            {
                // Use graph database features
                Execute.Sql(@"
                    CREATE TABLE GraphNodes (
                        Id INT IDENTITY PRIMARY KEY,
                        Name NVARCHAR(100)
                    ) AS NODE");
            }
        }
        else if (IfDatabase("Postgres"))
        {
            var version = GetPostgreSqlVersion();
            if (version >= new Version("9.4"))
            {
                // JSONB available in PostgreSQL 9.4+
                Alter.Table("AdvancedFeatures")
                    .AddColumn("JsonData").AsCustom("JSONB").Nullable();
            }
            else if (version >= new Version("9.2"))
            {
                // JSON available in PostgreSQL 9.2+
                Alter.Table("AdvancedFeatures")
                    .AddColumn("JsonData").AsCustom("JSON").Nullable();
            }
            else
            {
                // Fallback to TEXT
                Alter.Table("AdvancedFeatures")
                    .AddColumn("JsonData").AsCustom("TEXT").Nullable();
            }
        }
    }
    
    private Version GetSqlServerVersion()
    {
        try
        {
            var versionString = Execute.Sql("SELECT SERVERPROPERTY('ProductVersion')")
                .Returns<string>().FirstOrDefault();
            return Version.Parse(versionString ?? "11.0.0.0");
        }
        catch
        {
            return new Version("11.0.0.0"); // Default to SQL Server 2012
        }
    }
    
    private Version GetPostgreSqlVersion()
    {
        try
        {
            var versionString = Execute.Sql("SELECT version()")
                .Returns<string>().FirstOrDefault();
                
            // Extract version number from "PostgreSQL 13.2 on x86_64-pc-linux-gnu..."
            var match = System.Text.RegularExpressions.Regex.Match(versionString, @"PostgreSQL (\d+\.\d+)");
            if (match.Success)
            {
                return Version.Parse(match.Groups[1].Value + ".0");
            }
        }
        catch { }
        
        return new Version("9.0.0"); // Default to PostgreSQL 9.0
    }

    public override void Down()
    {
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP TABLE IF EXISTS GraphNodes");
        }
        Delete.Table("AdvancedFeatures");
    }
}
```

## Environment-Based Conditionals

### Environment Detection and Logic

```csharp
[Migration(202401151200)]
public class EnvironmentConditionals : Migration
{
    public override void Up()
    {
        var environment = GetCurrentEnvironment();
        
        Create.Table("EnvironmentSpecificTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Environment").AsString(50).NotNullable().WithDefaultValue(environment);
            
        switch (environment.ToLower())
        {
            case "development":
                SetupDevelopmentEnvironment();
                break;
                
            case "staging":
                SetupStagingEnvironment();
                break;
                
            case "production":
                SetupProductionEnvironment();
                break;
                
            case "test":
            case "testing":
                SetupTestEnvironment();
                break;
                
            default:
                SetupDefaultEnvironment();
                break;
        }
    }
    
    private string GetCurrentEnvironment()
    {
        // Try multiple environment variable names
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
               Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
               Environment.GetEnvironmentVariable("ENVIRONMENT") ??
               Environment.GetEnvironmentVariable("ENV") ??
               "Production"; // Default to most restrictive
    }
    
    private void SetupDevelopmentEnvironment()
    {
        Console.WriteLine("Setting up development environment features...");
        
        // Add development-specific columns
        Alter.Table("EnvironmentSpecificTable")
            .AddColumn("DebugInfo").AsString(1000).Nullable()
            .AddColumn("DeveloperNotes").AsString(2000).Nullable();
            
        // Insert development seed data
        var devUsers = new[]
        {
            new { Name = "Dev User 1", Environment = "development" },
            new { Name = "Dev User 2", Environment = "development" },
            new { Name = "Test Admin", Environment = "development" }
        };
        
        foreach (var user in devUsers)
        {
            Insert.IntoTable("EnvironmentSpecificTable").Row(user);
        }
        
        // Create development-specific indexes (less restrictive)
        Create.Index("IX_EnvironmentSpecificTable_Name_Dev")
            .OnTable("EnvironmentSpecificTable")
            .OnColumn("Name");
    }
    
    private void SetupStagingEnvironment()
    {
        Console.WriteLine("Setting up staging environment features...");
        
        // Add staging-specific columns
        Alter.Table("EnvironmentSpecificTable")
            .AddColumn("StagingFlags").AsString(500).Nullable()
            .AddColumn("TestScenarioId").AsInt32().Nullable();
            
        // Insert staging test data
        Insert.IntoTable("EnvironmentSpecificTable")
            .Row(new { Name = "Staging Test User", Environment = "staging" });
            
        // Create staging-specific indexes
        Create.Index("IX_EnvironmentSpecificTable_TestScenario")
            .OnTable("EnvironmentSpecificTable")
            .OnColumn("TestScenarioId");
    }
    
    private void SetupProductionEnvironment()
    {
        Console.WriteLine("Setting up production environment features...");
        
        // Add production-specific columns
        Alter.Table("EnvironmentSpecificTable")
            .AddColumn("AuditTrail").AsString(2000).NotNullable().WithDefaultValue("Created")
            .AddColumn("ComplianceFlags").AsString(200).Nullable();
            
        // Create production-optimized indexes
        Create.Index("IX_EnvironmentSpecificTable_Name_Production")
            .OnTable("EnvironmentSpecificTable")
            .OnColumn("Name");
            
        Create.Index("IX_EnvironmentSpecificTable_AuditTrail")
            .OnTable("EnvironmentSpecificTable")
            .OnColumn("AuditTrail");
            
        // Add production constraints
        Execute.Sql(@"
            ALTER TABLE EnvironmentSpecificTable 
            ADD CONSTRAINT CK_EnvironmentSpecificTable_Name_NotEmpty 
            CHECK (LEN(TRIM(Name)) > 0)");
            
        // No test data in production
        Console.WriteLine("Production environment: No test data inserted");
    }
    
    private void SetupTestEnvironment()
    {
        Console.WriteLine("Setting up test environment features...");
        
        // Add test-specific columns
        Alter.Table("EnvironmentSpecificTable")
            .AddColumn("TestCaseId").AsString(100).Nullable()
            .AddColumn("TestRunId").AsString(100).Nullable()
            .AddColumn("ExpectedResult").AsString(500).Nullable();
            
        // Insert comprehensive test data
        var testUsers = Enumerable.Range(1, 100)
            .Select(i => new { Name = $"Test User {i}", Environment = "test" });
            
        foreach (var user in testUsers)
        {
            Insert.IntoTable("EnvironmentSpecificTable").Row(user);
        }
    }
    
    private void SetupDefaultEnvironment()
    {
        Console.WriteLine("Setting up default environment features...");
        
        // Minimal, safe setup for unknown environments
        Create.Index("IX_EnvironmentSpecificTable_Name_Default")
            .OnTable("EnvironmentSpecificTable")
            .OnColumn("Name");
    }

    public override void Down()
    {
        Delete.Table("EnvironmentSpecificTable");
    }
}
```

### Configuration-Based Conditionals

```csharp
[Migration(202401151300)]
public class ConfigurationBasedConditionals : Migration
{
    public override void Up()
    {
        var config = GetMigrationConfiguration();
        
        Create.Table("ConfigurableFeatures")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
            
        // Apply features based on configuration flags
        if (config.EnableAuditingFeatures)
        {
            AddAuditingColumns();
        }
        
        if (config.EnableSecurityFeatures)
        {
            AddSecurityColumns();
        }
        
        if (config.EnablePerformanceOptimizations)
        {
            AddPerformanceOptimizations();
        }
        
        if (config.EnableComplianceFeatures)
        {
            AddComplianceFeatures();
        }
        
        // Apply database size optimizations
        ApplyOptimizationsForDatabaseSize(config.ExpectedDatabaseSize);
    }
    
    private MigrationConfiguration GetMigrationConfiguration()
    {
        return new MigrationConfiguration
        {
            EnableAuditingFeatures = GetConfigBool("ENABLE_AUDITING", true),
            EnableSecurityFeatures = GetConfigBool("ENABLE_SECURITY", true),
            EnablePerformanceOptimizations = GetConfigBool("ENABLE_PERFORMANCE_OPTS", true),
            EnableComplianceFeatures = GetConfigBool("ENABLE_COMPLIANCE", false),
            ExpectedDatabaseSize = GetConfigEnum<DatabaseSize>("DATABASE_SIZE", DatabaseSize.Medium),
            FeatureFlags = GetConfigString("FEATURE_FLAGS", "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        };
    }
    
    private bool GetConfigBool(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
    
    private T GetConfigEnum<T>(string key, T defaultValue) where T : struct, Enum
    {
        var value = Environment.GetEnvironmentVariable(key);
        return Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
    }
    
    private string GetConfigString(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }
    
    private void AddAuditingColumns()
    {
        Console.WriteLine("Adding auditing features...");
        
        Alter.Table("ConfigurableFeatures")
            .AddColumn("CreatedBy").AsString(100).NotNullable().WithDefaultValue("System")
            .AddColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .AddColumn("ModifiedBy").AsString(100).Nullable()
            .AddColumn("ModifiedAt").AsDateTime().Nullable()
            .AddColumn("VersionNumber").AsInt32().NotNullable().WithDefaultValue(1);
            
        // Create audit log table
        Create.Table("ConfigurableFeaturesAudit")
            .WithColumn("AuditId").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("RecordId").AsInt32().NotNullable()
            .WithColumn("Operation").AsString(20).NotNullable()
            .WithColumn("OldValues").AsString(4000).Nullable()
            .WithColumn("NewValues").AsString(4000).Nullable()
            .WithColumn("ChangedBy").AsString(100).NotNullable()
            .WithColumn("ChangedAt").AsDateTime().NotNullable();
    }
    
    private void AddSecurityColumns()
    {
        Console.WriteLine("Adding security features...");
        
        Alter.Table("ConfigurableFeatures")
            .AddColumn("SecurityClassification").AsString(50).NotNullable().WithDefaultValue("Internal")
            .AddColumn("AccessLevel").AsString(50).NotNullable().WithDefaultValue("Standard")
            .AddColumn("EncryptedData").AsBinary().Nullable()
            .AddColumn("HashValue").AsString(256).Nullable();
            
        // Add security constraints
        Execute.Sql(@"
            ALTER TABLE ConfigurableFeatures 
            ADD CONSTRAINT CK_ConfigurableFeatures_SecurityClassification 
            CHECK (SecurityClassification IN ('Public', 'Internal', 'Confidential', 'Restricted'))");
    }
    
    private void AddPerformanceOptimizations()
    {
        Console.WriteLine("Adding performance optimizations...");
        
        // Create performance-focused indexes
        Create.Index("IX_ConfigurableFeatures_Name_Optimized")
            .OnTable("ConfigurableFeatures")
            .OnColumn("Name");
            
        if (IfDatabase("SqlServer"))
        {
            // SQL Server specific optimizations
            Execute.Sql(@"
                CREATE INDEX IX_ConfigurableFeatures_CreatedAt_Covering 
                ON ConfigurableFeatures (CreatedAt) 
                INCLUDE (Name, CreatedBy)");
        }
        else if (IfDatabase("Postgres"))
        {
            // PostgreSQL specific optimizations
            Execute.Sql(@"
                CREATE INDEX CONCURRENTLY IX_ConfigurableFeatures_CreatedAt 
                ON ConfigurableFeatures (CreatedAt)");
        }
    }
    
    private void AddComplianceFeatures()
    {
        Console.WriteLine("Adding compliance features...");
        
        Alter.Table("ConfigurableFeatures")
            .AddColumn("ComplianceLevel").AsString(50).NotNullable().WithDefaultValue("Standard")
            .AddColumn("RetentionPeriod").AsInt32().NotNullable().WithDefaultValue(2555) // 7 years in days
            .AddColumn("DataClassification").AsString(100).NotNullable().WithDefaultValue("General")
            .AddColumn("PIIIndicator").AsBoolean().NotNullable().WithDefaultValue(false);
            
        // Create compliance audit table
        Create.Table("ComplianceAuditLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TableName").AsString(100).NotNullable()
            .WithColumn("RecordId").AsInt32().NotNullable()
            .WithColumn("ComplianceEvent").AsString(100).NotNullable()
            .WithColumn("EventData").AsString(2000).Nullable()
            .WithColumn("EventTimestamp").AsDateTime().NotNullable();
    }
    
    private void ApplyOptimizationsForDatabaseSize(DatabaseSize expectedSize)
    {
        Console.WriteLine($"Applying optimizations for {expectedSize} database size...");
        
        switch (expectedSize)
        {
            case DatabaseSize.Small:
                // Minimal indexing for small databases
                break;
                
            case DatabaseSize.Medium:
                // Standard indexing
                Create.Index("IX_ConfigurableFeatures_Standard")
                    .OnTable("ConfigurableFeatures")
                    .OnColumn("Name")
                    .OnColumn("CreatedAt");
                break;
                
            case DatabaseSize.Large:
                // Aggressive indexing and partitioning preparation
                Create.Index("IX_ConfigurableFeatures_Large1")
                    .OnTable("ConfigurableFeatures")
                    .OnColumn("Name");
                    
                Create.Index("IX_ConfigurableFeatures_Large2")
                    .OnTable("ConfigurableFeatures")
                    .OnColumn("CreatedAt");
                    
                Create.Index("IX_ConfigurableFeatures_Large3")
                    .OnTable("ConfigurableFeatures")
                    .OnColumn("SecurityClassification");
                break;
                
            case DatabaseSize.Enterprise:
                // Enterprise-level optimizations
                ApplyEnterpriseOptimizations();
                break;
        }
    }
    
    private void ApplyEnterpriseOptimizations()
    {
        if (IfDatabase("SqlServer"))
        {
            // SQL Server enterprise features
            Execute.Sql(@"
                CREATE NONCLUSTERED COLUMNSTORE INDEX IX_ConfigurableFeatures_Columnstore
                ON ConfigurableFeatures (Name, CreatedAt, CreatedBy)");
        }
        else if (IfDatabase("Postgres"))
        {
            // PostgreSQL enterprise features
            Execute.Sql("CREATE STATISTICS s_ConfigurableFeatures ON Name, CreatedAt FROM ConfigurableFeatures");
        }
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS ComplianceAuditLog");
        Execute.Sql("DROP TABLE IF EXISTS ConfigurableFeaturesAudit");
        Delete.Table("ConfigurableFeatures");
    }
}

public class MigrationConfiguration
{
    public bool EnableAuditingFeatures { get; set; }
    public bool EnableSecurityFeatures { get; set; }
    public bool EnablePerformanceOptimizations { get; set; }
    public bool EnableComplianceFeatures { get; set; }
    public DatabaseSize ExpectedDatabaseSize { get; set; }
    public string[] FeatureFlags { get; set; } = Array.Empty<string>();
}

public enum DatabaseSize
{
    Small,
    Medium, 
    Large,
    Enterprise
}
```

## Schema State Conditionals

### Conditional Based on Existing Schema

```csharp
[Migration(202401151400)]
public class SchemaStateConditionals : Migration
{
    public override void Up()
    {
        // Check if tables exist before creating or altering them
        if (!Schema.Table("Users").Exists())
        {
            CreateUsersTable();
        }
        else
        {
            ModifyExistingUsersTable();
        }
        
        // Conditional column addition based on existing columns
        if (!Schema.Table("Users").Column("Email").Exists())
        {
            Alter.Table("Users")
                .AddColumn("Email").AsString(255).Nullable();
        }
        
        // Conditional index creation
        if (!Schema.Table("Users").Index("IX_Users_Email").Exists())
        {
            Create.Index("IX_Users_Email")
                .OnTable("Users")
                .OnColumn("Email");
        }
        
        // Conditional constraint creation
        if (!Schema.Table("Users").Constraint("CK_Users_Email_Format").Exists())
        {
            Execute.Sql(@"
                ALTER TABLE Users 
                ADD CONSTRAINT CK_Users_Email_Format 
                CHECK (Email IS NULL OR Email LIKE '%@%')");
        }
        
        // Check for foreign key relationships
        if (Schema.Table("Orders").Exists() && 
            !Schema.Table("Orders").Constraint("FK_Orders_Users").Exists())
        {
            Create.ForeignKey("FK_Orders_Users")
                .FromTable("Orders").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id");
        }
        
        // Conditional data migration based on existing data
        ConditionalDataMigration();
    }
    
    private void CreateUsersTable()
    {
        Console.WriteLine("Creating Users table...");
        
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }
    
    private void ModifyExistingUsersTable()
    {
        Console.WriteLine("Modifying existing Users table...");
        
        // Only add columns that don't exist
        if (!Schema.Table("Users").Column("LastLoginAt").Exists())
        {
            Alter.Table("Users")
                .AddColumn("LastLoginAt").AsDateTime().Nullable();
        }
        
        if (!Schema.Table("Users").Column("IsActive").Exists())
        {
            Alter.Table("Users")
                .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
        }
        
        // Check if we need to modify existing columns
        ModifyExistingColumns();
    }
    
    private void ModifyExistingColumns()
    {
        // Check if Email column is too small
        var emailColumnInfo = GetColumnInfo("Users", "Email");
        if (emailColumnInfo != null && emailColumnInfo.MaxLength < 320) // RFC 5321 limit
        {
            Console.WriteLine("Expanding Email column size...");
            Alter.Column("Email").OnTable("Users")
                .AsString(320).Nullable();
        }
        
        // Check if Username column allows nulls (fix data integrity issue)
        if (emailColumnInfo != null && emailColumnInfo.IsNullable)
        {
            // First ensure no null values exist
            Execute.Sql("UPDATE Users SET Username = 'user' + CAST(Id AS VARCHAR) WHERE Username IS NULL");
            
            // Then make column not nullable
            Alter.Column("Username").OnTable("Users")
                .AsString(50).NotNullable();
        }
    }
    
    private void ConditionalDataMigration()
    {
        // Check if there's existing data that needs migration
        var userCount = Execute.Sql("SELECT COUNT(*) FROM Users").Returns<int>().FirstOrDefault();
        
        if (userCount > 0)
        {
            Console.WriteLine($"Performing data migration for {userCount} existing users...");
            
            // Check if any users are missing email addresses
            var usersWithoutEmail = Execute.Sql("SELECT COUNT(*) FROM Users WHERE Email IS NULL OR Email = ''")
                .Returns<int>().FirstOrDefault();
                
            if (usersWithoutEmail > 0)
            {
                Console.WriteLine($"Found {usersWithoutEmail} users without email addresses");
                
                // Generate placeholder emails for users without them
                Execute.Sql(@"
                    UPDATE Users 
                    SET Email = Username + '@placeholder.com'
                    WHERE Email IS NULL OR Email = ''");
            }
            
            // Check if LastLoginAt needs to be initialized
            var usersWithoutLastLogin = Execute.Sql("SELECT COUNT(*) FROM Users WHERE LastLoginAt IS NULL")
                .Returns<int>().FirstOrDefault();
                
            if (usersWithoutLastLogin > 0)
            {
                Console.WriteLine($"Initializing LastLoginAt for {usersWithoutLastLogin} users");
                
                // Set LastLoginAt to creation date for existing users
                Execute.Sql("UPDATE Users SET LastLoginAt = CreatedAt WHERE LastLoginAt IS NULL");
            }
        }
        else
        {
            Console.WriteLine("No existing users found, skipping data migration");
        }
    }
    
    private ColumnInfo GetColumnInfo(string tableName, string columnName)
    {
        try
        {
            if (IfDatabase("SqlServer"))
            {
                var result = Execute.Sql($@"
                    SELECT 
                        CHARACTER_MAXIMUM_LENGTH as MaxLength,
                        IS_NULLABLE as IsNullable,
                        DATA_TYPE as DataType
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'")
                    .Returns<dynamic>().FirstOrDefault();
                    
                if (result != null)
                {
                    return new ColumnInfo
                    {
                        MaxLength = result.MaxLength,
                        IsNullable = result.IsNullable == "YES",
                        DataType = result.DataType
                    };
                }
            }
            else if (IfDatabase("Postgres"))
            {
                var result = Execute.Sql($@"
                    SELECT 
                        character_maximum_length as MaxLength,
                        is_nullable as IsNullable,
                        data_type as DataType
                    FROM information_schema.columns 
                    WHERE table_name = '{tableName.ToLower()}' AND column_name = '{columnName.ToLower()}'")
                    .Returns<dynamic>().FirstOrDefault();
                    
                if (result != null)
                {
                    return new ColumnInfo
                    {
                        MaxLength = result.MaxLength,
                        IsNullable = result.IsNullable == "YES",
                        DataType = result.DataType
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not retrieve column info: {ex.Message}");
        }
        
        return null;
    }

    public override void Down()
    {
        // Conditional rollback - only drop if we created it
        if (Schema.Table("Users").Exists())
        {
            // Remove constraints we may have added
            Execute.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK_Users_Email_Format");
            
            // Remove indexes we may have added
            if (Schema.Table("Users").Index("IX_Users_Email").Exists())
            {
                Delete.Index("IX_Users_Email").OnTable("Users");
            }
            
            // Remove columns we may have added
            if (Schema.Table("Users").Column("LastLoginAt").Exists())
            {
                Delete.Column("LastLoginAt").FromTable("Users");
            }
            
            if (Schema.Table("Users").Column("IsActive").Exists())
            {
                Delete.Column("IsActive").FromTable("Users");
            }
        }
    }
}

public class ColumnInfo
{
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public string DataType { get; set; }
}
```

### Complex Schema Validation

```csharp
[Migration(202401151500)]
public class ComplexSchemaValidation : Migration
{
    public override void Up()
    {
        var validationResult = ValidateSchemaPrerequisites();
        
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Schema validation failed: {string.Join(", ", validationResult.Errors)}");
        }
        
        if (validationResult.HasWarnings)
        {
            Console.WriteLine($"Schema warnings: {string.Join(", ", validationResult.Warnings)}");
        }
        
        // Proceed with migration based on validation results
        ApplyMigrationBasedOnValidation(validationResult);
    }
    
    private SchemaValidationResult ValidateSchemaPrerequisites()
    {
        var result = new SchemaValidationResult();
        
        // Check required tables exist
        var requiredTables = new[] { "Users", "Roles" };
        foreach (var table in requiredTables)
        {
            if (!Schema.Table(table).Exists())
            {
                result.Errors.Add($"Required table '{table}' does not exist");
            }
        }
        
        // Check required columns exist
        if (Schema.Table("Users").Exists())
        {
            var requiredColumns = new[] { "Id", "Username", "Email" };
            foreach (var column in requiredColumns)
            {
                if (!Schema.Table("Users").Column(column).Exists())
                {
                    result.Errors.Add($"Required column 'Users.{column}' does not exist");
                }
            }
        }
        
        // Check for potential data integrity issues
        if (Schema.Table("Users").Exists() && Schema.Table("Orders").Exists())
        {
            var orphanedOrders = Execute.Sql(@"
                SELECT COUNT(*) 
                FROM Orders o 
                LEFT JOIN Users u ON o.UserId = u.Id 
                WHERE u.Id IS NULL")
                .Returns<int>().FirstOrDefault();
                
            if (orphanedOrders > 0)
            {
                result.Warnings.Add($"Found {orphanedOrders} orphaned orders without valid user references");
            }
        }
        
        // Check database constraints
        ValidateConstraints(result);
        
        // Check index coverage
        ValidateIndexCoverage(result);
        
        return result;
    }
    
    private void ValidateConstraints(SchemaValidationResult result)
    {
        if (IfDatabase("SqlServer"))
        {
            // Check for disabled constraints
            var disabledConstraints = Execute.Sql(@"
                SELECT COUNT(*) 
                FROM sys.check_constraints 
                WHERE is_disabled = 1")
                .Returns<int>().FirstOrDefault();
                
            if (disabledConstraints > 0)
            {
                result.Warnings.Add($"Found {disabledConstraints} disabled check constraints");
            }
            
            // Check for untrusted foreign keys
            var untrustedForeignKeys = Execute.Sql(@"
                SELECT COUNT(*) 
                FROM sys.foreign_keys 
                WHERE is_not_trusted = 1")
                .Returns<int>().FirstOrDefault();
                
            if (untrustedForeignKeys > 0)
            {
                result.Warnings.Add($"Found {untrustedForeignKeys} untrusted foreign key constraints");
            }
        }
    }
    
    private void ValidateIndexCoverage(SchemaValidationResult result)
    {
        if (Schema.Table("Users").Exists())
        {
            // Check if commonly queried columns are indexed
            var commonColumns = new[] { "Email", "Username", "CreatedAt" };
            foreach (var column in commonColumns)
            {
                if (Schema.Table("Users").Column(column).Exists())
                {
                    var indexExists = CheckColumnHasIndex("Users", column);
                    if (!indexExists)
                    {
                        result.Warnings.Add($"Column 'Users.{column}' is not indexed and may cause performance issues");
                    }
                }
            }
        }
    }
    
    private bool CheckColumnHasIndex(string tableName, string columnName)
    {
        try
        {
            if (IfDatabase("SqlServer"))
            {
                var indexCount = Execute.Sql($@"
                    SELECT COUNT(*)
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE OBJECT_NAME(i.object_id) = '{tableName}' 
                    AND c.name = '{columnName}'
                    AND i.is_primary_key = 0") // Exclude primary key
                    .Returns<int>().FirstOrDefault();
                    
                return indexCount > 0;
            }
        }
        catch
        {
            // If we can't check, assume it's not indexed
        }
        
        return false;
    }
    
    private void ApplyMigrationBasedOnValidation(SchemaValidationResult validationResult)
    {
        Console.WriteLine("Applying migration based on schema validation results...");
        
        // Create table that depends on validation
        Create.Table("ValidatedFeatures")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FeatureName").AsString(100).NotNullable()
            .WithColumn("ValidatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        // Apply conditional improvements based on warnings
        foreach (var warning in validationResult.Warnings)
        {
            if (warning.Contains("not indexed"))
            {
                Console.WriteLine("Adding recommended indexes based on validation warnings...");
                CreateRecommendedIndexes();
            }
            
            if (warning.Contains("orphaned"))
            {
                Console.WriteLine("Creating data cleanup procedures based on validation warnings...");
                CreateDataCleanupProcedures();
            }
        }
    }
    
    private void CreateRecommendedIndexes()
    {
        if (Schema.Table("Users").Column("Email").Exists() &&
            !CheckColumnHasIndex("Users", "Email"))
        {
            Create.Index("IX_Users_Email_Recommended")
                .OnTable("Users")
                .OnColumn("Email");
        }
        
        if (Schema.Table("Users").Column("CreatedAt").Exists() &&
            !CheckColumnHasIndex("Users", "CreatedAt"))
        {
            Create.Index("IX_Users_CreatedAt_Recommended")
                .OnTable("Users")
                .OnColumn("CreatedAt");
        }
    }
    
    private void CreateDataCleanupProcedures()
    {
        // Create stored procedure or function to clean up orphaned records
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql(@"
                CREATE PROCEDURE CleanupOrphanedOrders
                AS
                BEGIN
                    DELETE FROM Orders 
                    WHERE UserId NOT IN (SELECT Id FROM Users);
                    
                    SELECT @@ROWCOUNT as DeletedCount;
                END");
        }
    }

    public override void Down()
    {
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP PROCEDURE IF EXISTS CleanupOrphanedOrders");
        }
        
        Delete.Table("ValidatedFeatures");
    }
}

public class SchemaValidationResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();
}
```

## Custom Conditional Logic

### Feature Flag-Based Conditionals

```csharp
[Migration(202401151600)]
public class FeatureFlagConditionals : Migration
{
    public override void Up()
    {
        var featureFlags = GetFeatureFlags();
        
        Create.Table("FeatureBasedTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
            
        // Apply features based on flags
        if (featureFlags.Contains("ENHANCED_SEARCH"))
        {
            EnableEnhancedSearch();
        }
        
        if (featureFlags.Contains("ADVANCED_ANALYTICS"))
        {
            EnableAdvancedAnalytics();
        }
        
        if (featureFlags.Contains("REALTIME_NOTIFICATIONS"))
        {
            EnableRealtimeNotifications();
        }
        
        if (featureFlags.Contains("GDPR_COMPLIANCE"))
        {
            EnableGdprCompliance();
        }
        
        // Conditional based on multiple flags
        if (featureFlags.Contains("PREMIUM_FEATURES") && featureFlags.Contains("ENTERPRISE_SECURITY"))
        {
            EnablePremiumSecurityFeatures();
        }
    }
    
    private HashSet<string> GetFeatureFlags()
    {
        var flagsString = Environment.GetEnvironmentVariable("FEATURE_FLAGS") ?? "";
        return flagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim().ToUpper())
            .ToHashSet();
    }
    
    private void EnableEnhancedSearch()
    {
        Console.WriteLine("Enabling enhanced search features...");
        
        Alter.Table("FeatureBasedTable")
            .AddColumn("SearchKeywords").AsString(1000).Nullable()
            .AddColumn("SearchRanking").AsInt32().NotNullable().WithDefaultValue(0);
            
        // Create full-text search support
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("CREATE FULLTEXT CATALOG SearchCatalog AS DEFAULT");
            Execute.Sql(@"
                CREATE FULLTEXT INDEX ON FeatureBasedTable (SearchKeywords)
                KEY INDEX PK_FeatureBasedTable ON SearchCatalog");
        }
        else if (IfDatabase("Postgres"))
        {
            Execute.Sql("ALTER TABLE FeatureBasedTable ADD COLUMN SearchVector tsvector");
            Execute.Sql(@"
                CREATE INDEX IX_FeatureBasedTable_SearchVector 
                ON FeatureBasedTable USING GIN (SearchVector)");
        }
    }
    
    private void EnableAdvancedAnalytics()
    {
        Console.WriteLine("Enabling advanced analytics features...");
        
        // Create analytics tables
        Create.Table("AnalyticsEvents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("EventType").AsString(50).NotNullable()
            .WithColumn("EventData").AsString(2000).Nullable()
            .WithColumn("UserId").AsInt32().Nullable()
            .WithColumn("SessionId").AsString(100).Nullable()
            .WithColumn("Timestamp").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Table("AnalyticsSummary")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("SummaryDate").AsDate().NotNullable()
            .WithColumn("ViewCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("InteractionCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("UniqueUsers").AsInt32().NotNullable().WithDefaultValue(0);
            
        // Add analytics columns to main table
        Alter.Table("FeatureBasedTable")
            .AddColumn("AnalyticsEnabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("LastAnalyticsUpdate").AsDateTime().Nullable();
    }
    
    private void EnableRealtimeNotifications()
    {
        Console.WriteLine("Enabling realtime notifications features...");
        
        Create.Table("NotificationQueue")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("RecipientId").AsInt32().NotNullable()
            .WithColumn("NotificationType").AsString(50).NotNullable()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Message").AsString(1000).NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(5)
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Pending")
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("ScheduledFor").AsDateTime().Nullable()
            .WithColumn("SentAt").AsDateTime().Nullable();
            
        Create.Index("IX_NotificationQueue_Status_Priority")
            .OnTable("NotificationQueue")
            .OnColumn("Status")
            .OnColumn("Priority");
            
        Create.Index("IX_NotificationQueue_ScheduledFor")
            .OnTable("NotificationQueue")
            .OnColumn("ScheduledFor");
    }
    
    private void EnableGdprCompliance()
    {
        Console.WriteLine("Enabling GDPR compliance features...");
        
        // Add GDPR-related columns to main table
        Alter.Table("FeatureBasedTable")
            .AddColumn("PersonalDataFields").AsString(500).Nullable()
            .AddColumn("ConsentGranted").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("ConsentDate").AsDateTime().Nullable()
            .AddColumn("DataRetentionDays").AsInt32().NotNullable().WithDefaultValue(2555) // 7 years
            .AddColumn("ScheduledForDeletion").AsDateTime().Nullable();
            
        // Create GDPR audit table
        Create.Table("GdprAuditLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EntityType").AsString(100).NotNullable()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("Action").AsString(50).NotNullable() // CONSENT_GRANTED, DATA_EXPORTED, DATA_DELETED
            .WithColumn("RequestedBy").AsInt32().Nullable()
            .WithColumn("ProcessedBy").AsString(100).NotNullable()
            .WithColumn("ProcessedAt").AsDateTime().NotNullable()
            .WithColumn("Details").AsString(1000).Nullable();
            
        // Create data export requests table
        Create.Table("DataExportRequests")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("RequestedBy").AsInt32().NotNullable()
            .WithColumn("RequestType").AsString(50).NotNullable() // EXPORT, DELETE
            .WithColumn("Status").AsString(20).NotNullable().WithDefaultValue("Pending")
            .WithColumn("RequestedAt").AsDateTime().NotNullable()
            .WithColumn("CompletedAt").AsDateTime().Nullable()
            .WithColumn("ExportFilePath").AsString(500).Nullable();
    }
    
    private void EnablePremiumSecurityFeatures()
    {
        Console.WriteLine("Enabling premium security features...");
        
        // Add security audit columns
        Alter.Table("FeatureBasedTable")
            .AddColumn("SecurityLevel").AsInt32().NotNullable().WithDefaultValue(1)
            .AddColumn("LastSecurityScan").AsDateTime().Nullable()
            .AddColumn("EncryptionStatus").AsString(50).NotNullable().WithDefaultValue("None")
            .AddColumn("AccessControlHash").AsString(256).Nullable();
            
        // Create security events table
        Create.Table("SecurityEvents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EntityType").AsString(100).NotNullable()
            .WithColumn("EntityId").AsInt32().NotNullable()
            .WithColumn("EventType").AsString(50).NotNullable()
            .WithColumn("Severity").AsString(20).NotNullable()
            .WithColumn("Description").AsString(1000).NotNullable()
            .WithColumn("UserId").AsInt32().Nullable()
            .WithColumn("IPAddress").AsString(45).Nullable()
            .WithColumn("UserAgent").AsString(500).Nullable()
            .WithColumn("OccurredAt").AsDateTime().NotNullable();
            
        Create.Index("IX_SecurityEvents_Severity_OccurredAt")
            .OnTable("SecurityEvents")
            .OnColumn("Severity")
            .OnColumn("OccurredAt");
    }

    public override void Down()
    {
        // Clean up feature-specific tables and columns
        Execute.Sql("DROP TABLE IF EXISTS SecurityEvents");
        Execute.Sql("DROP TABLE IF EXISTS DataExportRequests");
        Execute.Sql("DROP TABLE IF EXISTS GdprAuditLog");
        Execute.Sql("DROP TABLE IF EXISTS NotificationQueue");
        Execute.Sql("DROP TABLE IF EXISTS AnalyticsSummary");
        Execute.Sql("DROP TABLE IF EXISTS AnalyticsEvents");
        
        if (IfDatabase("SqlServer"))
        {
            Execute.Sql("DROP FULLTEXT INDEX ON FeatureBasedTable");
            Execute.Sql("DROP FULLTEXT CATALOG SearchCatalog");
        }
        
        Delete.Table("FeatureBasedTable");
    }
}
```

### Time-Based Conditionals

```csharp
[Migration(202401151700)]
public class TimeBasedConditionals : Migration
{
    public override void Up()
    {
        var currentDate = DateTime.UtcNow;
        var deploymentPhase = GetDeploymentPhase(currentDate);
        
        Console.WriteLine($"Applying migration for deployment phase: {deploymentPhase}");
        
        Create.Table("TimeBasedFeatures")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FeatureName").AsString(100).NotNullable()
            .WithColumn("EnabledAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("Phase").AsString(50).NotNullable().WithDefaultValue(deploymentPhase.ToString());
            
        switch (deploymentPhase)
        {
            case DeploymentPhase.PreLaunch:
                ApplyPreLaunchFeatures();
                break;
                
            case DeploymentPhase.SoftLaunch:
                ApplySoftLaunchFeatures();
                break;
                
            case DeploymentPhase.FullLaunch:
                ApplyFullLaunchFeatures();
                break;
                
            case DeploymentPhase.Maintenance:
                ApplyMaintenanceFeatures();
                break;
        }
        
        // Apply features based on specific dates
        ApplyDateBasedFeatures(currentDate);
    }
    
    private DeploymentPhase GetDeploymentPhase(DateTime currentDate)
    {
        // Define phase dates (these would typically come from configuration)
        var softLaunchDate = new DateTime(2024, 2, 1);
        var fullLaunchDate = new DateTime(2024, 3, 1);
        var maintenanceStartDate = new DateTime(2024, 6, 1);
        
        if (currentDate < softLaunchDate)
            return DeploymentPhase.PreLaunch;
        else if (currentDate < fullLaunchDate)
            return DeploymentPhase.SoftLaunch;
        else if (currentDate < maintenanceStartDate)
            return DeploymentPhase.FullLaunch;
        else
            return DeploymentPhase.Maintenance;
    }
    
    private void ApplyPreLaunchFeatures()
    {
        Console.WriteLine("Applying pre-launch features...");
        
        // Basic features only
        Alter.Table("TimeBasedFeatures")
            .AddColumn("IsEnabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("TestingNotes").AsString(1000).Nullable();
    }
    
    private void ApplySoftLaunchFeatures()
    {
        Console.WriteLine("Applying soft launch features...");
        
        // Limited user base features
        Alter.Table("TimeBasedFeatures")
            .AddColumn("IsEnabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("MaxUsers").AsInt32().NotNullable().WithDefaultValue(1000)
            .AddColumn("BetaUserOnly").AsBoolean().NotNullable().WithDefaultValue(true);
            
        // Create beta user tracking
        Create.Table("BetaUsers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("InvitedAt").AsDateTime().NotNullable()
            .WithColumn("AcceptedAt").AsDateTime().Nullable()
            .WithColumn("FeedbackProvided").AsBoolean().NotNullable().WithDefaultValue(false);
    }
    
    private void ApplyFullLaunchFeatures()
    {
        Console.WriteLine("Applying full launch features...");
        
        // All features enabled
        Alter.Table("TimeBasedFeatures")
            .AddColumn("IsEnabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("MaxUsers").AsInt32().Nullable() // No limit
            .AddColumn("PubliclyAvailable").AsBoolean().NotNullable().WithDefaultValue(true);
            
        // Create full user analytics
        Create.Table("UserAnalytics")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("FeatureUsage").AsString(2000).Nullable()
            .WithColumn("LastActive").AsDateTime().NotNullable()
            .WithColumn("TotalSessions").AsInt32().NotNullable().WithDefaultValue(0);
    }
    
    private void ApplyMaintenanceFeatures()
    {
        Console.WriteLine("Applying maintenance phase features...");
        
        // Add maintenance and monitoring features
        Alter.Table("TimeBasedFeatures")
            .AddColumn("MaintenanceMode").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("LastMaintenance").AsDateTime().Nullable()
            .AddColumn("PerformanceMetrics").AsString(1000).Nullable();
            
        // Create maintenance log
        Create.Table("MaintenanceLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("MaintenanceType").AsString(100).NotNullable()
            .WithColumn("StartTime").AsDateTime().NotNullable()
            .WithColumn("EndTime").AsDateTime().Nullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Notes").AsString(2000).Nullable();
    }
    
    private void ApplyDateBasedFeatures(DateTime currentDate)
    {
        // Enable specific features based on exact dates
        if (currentDate >= new DateTime(2024, 1, 15))
        {
            Console.WriteLine("Enabling new year features...");
            
            Alter.Table("TimeBasedFeatures")
                .AddColumn("NewYearFeature").AsBoolean().NotNullable().WithDefaultValue(true);
        }
        
        // Seasonal features
        var month = currentDate.Month;
        if (month >= 11 || month <= 1) // November to January
        {
            Console.WriteLine("Enabling holiday features...");
            
            Alter.Table("TimeBasedFeatures")
                .AddColumn("HolidayTheme").AsString(50).NotNullable().WithDefaultValue("Winter");
        }
        else if (month >= 6 && month <= 8) // June to August
        {
            Console.WriteLine("Enabling summer features...");
            
            Alter.Table("TimeBasedFeatures")
                .AddColumn("SummerPromotion").AsBoolean().NotNullable().WithDefaultValue(true);
        }
        
        // Compliance deadline features
        if (currentDate >= new DateTime(2024, 5, 25)) // GDPR anniversary
        {
            Console.WriteLine("Enabling enhanced privacy features...");
            
            Alter.Table("TimeBasedFeatures")
                .AddColumn("EnhancedPrivacy").AsBoolean().NotNullable().WithDefaultValue(true)
                .AddColumn("DataRetentionDays").AsInt32().NotNullable().WithDefaultValue(365);
        }
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS MaintenanceLog");
        Execute.Sql("DROP TABLE IF EXISTS UserAnalytics");
        Execute.Sql("DROP TABLE IF EXISTS BetaUsers");
        Delete.Table("TimeBasedFeatures");
    }
}

public enum DeploymentPhase
{
    PreLaunch,
    SoftLaunch,
    FullLaunch,
    Maintenance
}
```

## Best Practices for Conditional Logic

### ✅ Do:
- Use database provider conditionals for database-specific optimizations
- Implement environment-based logic for safe deployment practices
- Validate schema state before making assumptions
- Use feature flags for gradual rollouts
- Document all conditional logic clearly
- Test all conditional paths thoroughly

### ❌ Don't:
- Rely on conditionals for core business logic that should be in application code
- Create overly complex nested conditions that are hard to maintain
- Skip error handling in conditional branches
- Make assumptions about schema state without validation
- Use conditionals to work around poor design decisions

Conditional logic in migrations enables powerful, adaptive database evolution while maintaining compatibility across different environments and scenarios.

## See Also

- [Best Practices](best-practices.md)
- [Migration Versioning](versioning.md)
- [Edge Cases and Troubleshooting](edge-cases.md)
- [Database Providers](../providers/)
- [Common Operations](../operations/)