# Migration Versioning

Effective migration versioning is crucial for maintaining database schema consistency across environments and team members. This guide covers versioning strategies, numbering schemes, and best practices for managing migration versions in FluentMigrator.

## Version Numbering Strategies

### 1. Timestamp-Based Versioning (Recommended)

The most common and recommended approach uses timestamps in the format `YYYYMMDDHHNN`:

```csharp
[Migration(202401151430)] // January 15, 2024, 14:30
public class AddUserEmailColumn : Migration
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

[Migration(202401151435)] // January 15, 2024, 14:35
public class AddUserEmailIndex : Migration
{
    public override void Up()
    {
        Create.Index("IX_Users_Email")
            .OnTable("Users")
            .OnColumn("Email")
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("IX_Users_Email").OnTable("Users");
    }
}
```

#### Advantages:
- Natural chronological ordering
- Easy to understand when migration was created
- Minimal conflicts in team environments
- Self-documenting timeline

#### Best Practices for Timestamps:
```csharp
// Use UTC time to avoid timezone confusion
[Migration(202401151430)] // Use UTC timestamp

// Leave gaps for emergency fixes
[Migration(202401151400)] // Main feature
[Migration(202401151405)] // Related changes
// Gap available for hotfix if needed: 202401151410-202401151459

// Group related migrations by time proximity
[Migration(202401151500)] // Start of new feature
[Migration(202401151505)] // Continue feature
[Migration(202401151510)] // Complete feature
```

### 2. Sequential Numbering

Simple incremental numbering approach:

```csharp
[Migration(1)]
public class CreateUserTable : Migration { /* ... */ }

[Migration(2)]
public class AddUserEmailColumn : Migration { /* ... */ }

[Migration(3)]
public class CreateProductTable : Migration { /* ... */ }
```

#### When to Use Sequential:
- Small teams with tight coordination
- Single developer projects
- When you need simple, predictable ordering

#### Challenges:
- High conflict potential in team environments
- No temporal information
- Difficult to insert emergency fixes

### 3. Semantic Versioning Integration

Combine semantic version with sequential numbers:

```csharp
[Migration(10001)] // Version 1.0.0, Migration 1
public class CreateInitialSchema : Migration { /* ... */ }

[Migration(10002)] // Version 1.0.0, Migration 2
public class AddBasicIndexes : Migration { /* ... */ }

[Migration(11001)] // Version 1.1.0, Migration 1
public class AddUserProfiles : Migration { /* ... */ }

[Migration(20001)] // Version 2.0.0, Migration 1
public class MajorSchemaRefactor : Migration { /* ... */ }
```

### 4. Feature-Based Versioning

Group migrations by feature with sub-versions:

```csharp
// User Management Feature (100xxx)
[Migration(100001)]
public class CreateUserTable : Migration { /* ... */ }

[Migration(100002)]
public class AddUserRoles : Migration { /* ... */ }

[Migration(100003)]
public class AddUserPreferences : Migration { /* ... */ }

// Product Catalog Feature (200xxx)
[Migration(200001)]
public class CreateProductTables : Migration { /* ... */ }

[Migration(200002)]
public class AddProductCategories : Migration { /* ... */ }

// Order Management Feature (300xxx)
[Migration(300001)]
public class CreateOrderTables : Migration { /* ... */ }
```

## Version Management Patterns

### 1. Development Branch Versioning

```csharp
public class MigrationVersionHelper
{
    public static long GenerateVersion()
    {
        // Generate timestamp-based version
        var now = DateTime.UtcNow;
        return long.Parse(now.ToString("yyyyMMddHHmm"));
    }
    
    public static long GenerateFeatureVersion(int featureId, int sequenceNumber)
    {
        // Feature-based versioning: FFFSSS (Feature + Sequence)
        return (featureId * 1000) + sequenceNumber;
    }
    
    public static bool IsValidVersion(long version)
    {
        // Validate timestamp format
        var versionStr = version.ToString();
        if (versionStr.Length != 12) return false;
        
        if (!DateTime.TryParseExact(versionStr, "yyyyMMddHHmm", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return false;
            
        return true;
    }
}

// Usage example
[Migration(MigrationVersionHelper.GenerateVersion())]
public class AddUserLocationData : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Country").AsString(50).Nullable()
            .AddColumn("City").AsString(100).Nullable();
    }

    public override void Down()
    {
        Delete.Column("City").FromTable("Users");
        Delete.Column("Country").FromTable("Users");
    }
}
```

### 2. Environment-Specific Versioning

```csharp
[Migration(202401151600)]
public class EnvironmentAwareMigration : Migration
{
    public override void Up()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        // Core changes applied to all environments
        Create.Table("AuditLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TableName").AsString(100).NotNullable()
            .WithColumn("Action").AsString(50).NotNullable()
            .WithColumn("Timestamp").AsDateTime().NotNullable();
            
        // Environment-specific data
        switch (environment?.ToLower())
        {
            case "development":
                Insert.IntoTable("AuditLog")
                    .Row(new { TableName = "Users", Action = "DEV_SEED_DATA", Timestamp = DateTime.Now });
                break;
                
            case "staging":
                Insert.IntoTable("AuditLog")
                    .Row(new { TableName = "Users", Action = "STAGING_SETUP", Timestamp = DateTime.Now });
                break;
                
            case "production":
                // Production-specific setup
                Execute.Sql("CREATE INDEX IX_AuditLog_Timestamp ON AuditLog (Timestamp) WITH (ONLINE = ON)");
                break;
        }
    }

    public override void Down()
    {
        Delete.Table("AuditLog");
    }
}
```

### 3. Hotfix and Emergency Migrations

```csharp
public static class MigrationVersioning
{
    // Standard migration numbering
    public static long StandardVersion(DateTime created) => 
        long.Parse(created.ToString("yyyyMMddHHmm"));
    
    // Hotfix numbering - use seconds for fine-grained control
    public static long HotfixVersion(DateTime created) => 
        long.Parse(created.ToString("yyyyMMddHHmmss"));
    
    // Emergency patch - use milliseconds for immediate fixes
    public static long EmergencyVersion(DateTime created) => 
        long.Parse(created.ToString("yyyyMMddHHmmssf"));
}

// Regular migration
[Migration(202401151500)]
public class AddUserPreferences : Migration { /* ... */ }

// Emergency hotfix after the above migration
[Migration(20240115150001)] // Added seconds for emergency fix
public class FixUserPreferencesDefault : Migration
{
    public override void Up()
    {
        // Emergency fix for production issue
        Execute.Sql("UPDATE Users SET Preferences = '{}' WHERE Preferences IS NULL");
    }

    public override void Down()
    {
        Execute.Sql("UPDATE Users SET Preferences = NULL WHERE Preferences = '{}'");
    }
}
```

## Version Conflict Resolution

### 1. Team Collaboration Patterns

```csharp
public class TeamVersioningStrategy
{
    // Developer-specific version ranges to avoid conflicts
    public static class DeveloperRanges
    {
        public const long ALICE_START = 202401010000;   // Alice: xx01xx
        public const long BOB_START = 202401020000;     // Bob: xx02xx  
        public const long CHARLIE_START = 202401030000; // Charlie: xx03xx
    }
    
    public static long GenerateVersionForDeveloper(string developerName)
    {
        var baseDate = DateTime.UtcNow.ToString("yyyyMM");
        var dayOfMonth = DateTime.UtcNow.Day.ToString("00");
        var timeStamp = DateTime.UtcNow.ToString("HHmm");
        
        var developerCode = developerName.ToLower() switch
        {
            "alice" => "01",
            "bob" => "02", 
            "charlie" => "03",
            _ => "99" // Generic/shared migrations
        };
        
        return long.Parse($"{baseDate}{developerCode}{timeStamp}");
    }
}

// Usage
[Migration(202401011430)] // Alice's migration on day 01
public class AddUserEmailByAlice : Migration { /* ... */ }

[Migration(202401021430)] // Bob's migration on day 02, same time - no conflict
public class AddProductsByBob : Migration { /* ... */ }
```

### 2. Conflict Detection and Resolution

```csharp
public class MigrationConflictDetector
{
    private readonly IMigrationRunner _runner;
    
    public MigrationConflictDetector(IMigrationRunner runner)
    {
        _runner = runner;
    }
    
    public List<MigrationConflict> DetectConflicts(Assembly migrationAssembly)
    {
        var conflicts = new List<MigrationConflict>();
        var migrations = migrationAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)))
            .Select(t => new
            {
                Type = t,
                Version = t.GetCustomAttribute<MigrationAttribute>()?.Version ?? 0,
                Name = t.Name
            })
            .OrderBy(m => m.Version)
            .ToList();
            
        // Check for duplicate versions
        var duplicates = migrations
            .GroupBy(m => m.Version)
            .Where(g => g.Count() > 1)
            .ToList();
            
        foreach (var duplicate in duplicates)
        {
            conflicts.Add(new MigrationConflict
            {
                ConflictType = ConflictType.DuplicateVersion,
                Version = duplicate.Key,
                AffectedMigrations = duplicate.Select(d => d.Name).ToList()
            });
        }
        
        // Check for out-of-order migrations
        var appliedVersions = GetAppliedMigrationVersions();
        var newMigrations = migrations.Where(m => !appliedVersions.Contains(m.Version)).ToList();
        
        foreach (var newMigration in newMigrations)
        {
            var lowerVersionsApplied = appliedVersions.Where(v => v > newMigration.Version).ToList();
            if (lowerVersionsApplied.Any())
            {
                conflicts.Add(new MigrationConflict
                {
                    ConflictType = ConflictType.OutOfOrder,
                    Version = newMigration.Version,
                    AffectedMigrations = new List<string> { newMigration.Name },
                    Message = $"Migration {newMigration.Version} is out of order. Higher versions already applied: {string.Join(", ", lowerVersionsApplied)}"
                });
            }
        }
        
        return conflicts;
    }
    
    private List<long> GetAppliedMigrationVersions()
    {
        // Get versions from VersionInfo table
        return Execute.Sql("SELECT Version FROM VersionInfo ORDER BY Version")
            .Returns<long>().ToList();
    }
}

public class MigrationConflict
{
    public ConflictType ConflictType { get; set; }
    public long Version { get; set; }
    public List<string> AffectedMigrations { get; set; }
    public string Message { get; set; }
}

public enum ConflictType
{
    DuplicateVersion,
    OutOfOrder,
    MissingDependency
}
```

### 3. Migration Rebasing Strategy

```csharp
public class MigrationRebaseHelper
{
    public static void RebaseConflictingMigrations(string migrationDirectory)
    {
        var migrationFiles = Directory.GetFiles(migrationDirectory, "*.cs")
            .Where(f => Path.GetFileName(f).Contains("Migration"))
            .ToList();
            
        foreach (var file in migrationFiles)
        {
            var content = File.ReadAllText(file);
            var currentVersion = ExtractVersionFromFile(content);
            
            if (IsConflictingVersion(currentVersion))
            {
                var newVersion = GenerateNewVersion();
                var updatedContent = ReplaceVersion(content, currentVersion, newVersion);
                
                // Backup original file
                File.Copy(file, file + ".backup");
                
                // Write updated file
                File.WriteAllText(file, updatedContent);
                
                Console.WriteLine($"Rebased migration {Path.GetFileName(file)}: {currentVersion} -> {newVersion}");
            }
        }
    }
    
    private static long ExtractVersionFromFile(string content)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content, @"\[Migration\((\d+)\)\]");
        return match.Success ? long.Parse(match.Groups[1].Value) : 0;
    }
    
    private static bool IsConflictingVersion(long version)
    {
        // Check if version conflicts with existing migrations
        // This would typically query the database or check other migration files
        return false; // Simplified for example
    }
    
    private static long GenerateNewVersion()
    {
        return long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmm"));
    }
    
    private static string ReplaceVersion(string content, long oldVersion, long newVersion)
    {
        return content.Replace($"[Migration({oldVersion})]", $"[Migration({newVersion})]");
    }
}
```

## Advanced Versioning Scenarios

### 1. Multi-Branch Development

```csharp
public class MultiBranchVersioning
{
    // Branch-specific version prefixes
    public static class BranchPrefixes
    {
        public const long MAIN_BRANCH = 1000000000000L;      // 1xxxxxxxxxxxxx
        public const long FEATURE_BRANCH = 2000000000000L;   // 2xxxxxxxxxxxxx
        public const long HOTFIX_BRANCH = 3000000000000L;    // 3xxxxxxxxxxxxx
        public const long RELEASE_BRANCH = 4000000000000L;   // 4xxxxxxxxxxxxx
    }
    
    public static long GenerateBranchVersion(string branchType)
    {
        var timestamp = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmm"));
        
        return branchType.ToLower() switch
        {
            "main" => MAIN_BRANCH + timestamp,
            "feature" => FEATURE_BRANCH + timestamp,
            "hotfix" => HOTFIX_BRANCH + timestamp,
            "release" => RELEASE_BRANCH + timestamp,
            _ => timestamp
        };
    }
}

// Feature branch migration
[Migration(2202401151430)] // Feature branch prefix + timestamp
public class FeatureBranchMigration : Migration
{
    public override void Up()
    {
        // Feature-specific changes
        Create.Table("FeatureTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("FeatureData").AsString(500).Nullable();
    }

    public override void Down()
    {
        Delete.Table("FeatureTable");
    }
}

// Hotfix migration
[Migration(3202401151445)] // Hotfix branch prefix + timestamp
public class CriticalSecurityFix : Migration
{
    public override void Up()
    {
        // Critical security patch
        Execute.Sql("UPDATE Users SET PasswordHash = NULL WHERE PasswordHash = ''");
    }

    public override void Down()
    {
        // Careful with security rollbacks
        Console.WriteLine("WARNING: Rolling back security fix!");
    }
}
```

### 2. Database Schema Branching

```csharp
[Migration(202401151700)]
public class SchemaVersionTracking : Migration
{
    public override void Up()
    {
        // Create schema versioning table
        Create.Table("SchemaVersions")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("MajorVersion").AsInt32().NotNullable()
            .WithColumn("MinorVersion").AsInt32().NotNullable()
            .WithColumn("PatchVersion").AsInt32().NotNullable()
            .WithColumn("BuildVersion").AsInt32().NotNullable()
            .WithColumn("BranchName").AsString(100).NotNullable()
            .WithColumn("CommitHash").AsString(40).Nullable()
            .WithColumn("AppliedAt").AsDateTime().NotNullable()
            .WithColumn("AppliedBy").AsString(100).NotNullable();
            
        // Record this schema version
        var version = GetCurrentSchemaVersion();
        Insert.IntoTable("SchemaVersions")
            .Row(new
            {
                MajorVersion = version.Major,
                MinorVersion = version.Minor,
                PatchVersion = version.Patch,
                BuildVersion = version.Build,
                BranchName = GetCurrentBranch(),
                CommitHash = GetCurrentCommit(),
                AppliedAt = DateTime.UtcNow,
                AppliedBy = Environment.UserName
            });
    }
    
    private SchemaVersion GetCurrentSchemaVersion()
    {
        // Extract version from assembly or configuration
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        return new SchemaVersion
        {
            Major = version.Major,
            Minor = version.Minor,
            Patch = version.Build,
            Build = version.Revision
        };
    }
    
    private string GetCurrentBranch()
    {
        // Get current git branch
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch --show-current",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(result) ? "unknown" : result;
        }
        catch
        {
            return "unknown";
        }
    }
    
    private string GetCurrentCommit()
    {
        // Get current git commit hash
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(result) ? null : result[..8]; // Short hash
        }
        catch
        {
            return null;
        }
    }

    public override void Down()
    {
        Delete.Table("SchemaVersions");
    }
}

public class SchemaVersion
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public int Build { get; set; }
    
    public override string ToString() => $"{Major}.{Minor}.{Patch}.{Build}";
}
```

### 3. Conditional Version Application

```csharp
[Migration(202401151800)]
public class ConditionalVersionMigration : Migration
{
    public override void Up()
    {
        var currentVersion = GetCurrentSchemaVersion();
        var targetVersion = new Version("2.1.0");
        
        // Only apply if upgrading from version 2.0.x to 2.1.x
        if (currentVersion.Major == 2 && currentVersion.Minor == 0)
        {
            // Breaking changes for major version upgrade
            Execute.Sql("-- Major version migration logic");
            
            // Add new required columns with default values
            Alter.Table("Users")
                .AddColumn("ApiVersion").AsString(10).NotNullable().WithDefaultValue("2.1");
                
            // Update existing data for compatibility
            Execute.Sql("UPDATE Users SET ApiVersion = '2.0' WHERE CreatedAt < '2024-01-01'");
        }
        else if (currentVersion < targetVersion)
        {
            // Minor version upgrade
            Execute.Sql("-- Minor version migration logic");
            
            Alter.Table("Users")
                .AddColumn("FeatureFlag").AsBoolean().NotNullable().WithDefaultValue(false);
        }
        
        // Update version tracking
        UpdateSchemaVersion(targetVersion);
    }
    
    private Version GetCurrentSchemaVersion()
    {
        try
        {
            var versionString = Execute.Sql(@"
                SELECT TOP 1 CONCAT(MajorVersion, '.', MinorVersion, '.', PatchVersion)
                FROM SchemaVersions 
                ORDER BY AppliedAt DESC")
                .Returns<string>().FirstOrDefault();
                
            return string.IsNullOrEmpty(versionString) ? new Version("1.0.0") : new Version(versionString);
        }
        catch
        {
            return new Version("1.0.0");
        }
    }
    
    private void UpdateSchemaVersion(Version version)
    {
        Insert.IntoTable("SchemaVersions")
            .Row(new
            {
                MajorVersion = version.Major,
                MinorVersion = version.Minor,
                PatchVersion = version.Build,
                BuildVersion = version.Revision,
                BranchName = "main",
                AppliedAt = DateTime.UtcNow,
                AppliedBy = Environment.UserName
            });
    }

    public override void Down()
    {
        // Conditional rollback logic
        var currentVersion = GetCurrentSchemaVersion();
        
        if (currentVersion.Major == 2 && currentVersion.Minor == 1)
        {
            Delete.Column("ApiVersion").FromTable("Users");
        }
        
        Delete.Column("FeatureFlag").FromTable("Users");
    }
}
```

## Version Documentation and Tracking

### 1. Migration Documentation Standards

```csharp
/// <summary>
/// Migration: Add user notification preferences system
/// Version: 202401151900
/// Author: John Doe (john.doe@company.com)
/// Date: 2024-01-15
/// 
/// Purpose:
/// Implements user notification preferences to support the new notification
/// system being released in version 2.3.0 of the application.
/// 
/// Changes:
/// - Creates NotificationPreferences table
/// - Adds foreign key relationship to Users table
/// - Migrates existing users to default notification settings
/// - Creates indexes for performance optimization
/// 
/// Breaking Changes: None
/// Data Migration: Yes - creates default preferences for existing users
/// Rollback Strategy: Drops table and restores original state
/// 
/// Dependencies:
/// - Requires Users table (created in migration 202401010001)
/// - Must be applied before NotificationSystem migration (202401152000)
/// 
/// Testing:
/// - Verified with 1M+ user records in staging
/// - Performance tested: < 30 seconds execution time
/// - Rollback tested and verified
/// 
/// Related:
/// - Story: US-1234 "Implement User Notification Preferences"
/// - Epic: EPIC-56 "Notification System Overhaul"
/// - Documentation: /docs/notifications/preferences.md
/// </summary>
[Migration(202401151900)]
public class AddUserNotificationPreferences : Migration
{
    private const string MIGRATION_PURPOSE = "Add user notification preferences system";
    private const string STORY_REFERENCE = "US-1234";
    
    public override void Up()
    {
        LogMigrationStart();
        
        try
        {
            // Create notification preferences table
            Create.Table("NotificationPreferences")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("EmailNotifications").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("SmsNotifications").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("PushNotifications").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("NotificationFrequency").AsString(20).NotNullable().WithDefaultValue("Daily")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
                
            // Create foreign key relationship
            Create.ForeignKey("FK_NotificationPreferences_Users")
                .FromTable("NotificationPreferences").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);
                
            // Create performance indexes
            Create.Index("IX_NotificationPreferences_UserId")
                .OnTable("NotificationPreferences")
                .OnColumn("UserId")
                .Unique(); // One preference record per user
                
            // Migrate existing users to default settings
            Execute.Sql(@"
                INSERT INTO NotificationPreferences (UserId, EmailNotifications, SmsNotifications, PushNotifications, NotificationFrequency, CreatedAt, UpdatedAt)
                SELECT Id, 1, 0, 1, 'Daily', GETDATE(), GETDATE()
                FROM Users 
                WHERE Id NOT IN (SELECT UserId FROM NotificationPreferences)");
                
            LogMigrationSuccess();
        }
        catch (Exception ex)
        {
            LogMigrationFailure(ex);
            throw;
        }
    }

    public override void Down()
    {
        LogRollbackStart();
        
        Delete.ForeignKey("FK_NotificationPreferences_Users").OnTable("NotificationPreferences");
        Delete.Table("NotificationPreferences");
        
        LogRollbackSuccess();
    }
    
    private void LogMigrationStart()
    {
        Console.WriteLine($"Starting migration: {MIGRATION_PURPOSE}");
        Console.WriteLine($"Story: {STORY_REFERENCE}");
        Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    }
    
    private void LogMigrationSuccess()
    {
        Console.WriteLine($"Migration completed successfully: {MIGRATION_PURPOSE}");
        
        // Log user count for verification
        var userCount = Execute.Sql("SELECT COUNT(*) FROM Users").Returns<int>().FirstOrDefault();
        var prefsCount = Execute.Sql("SELECT COUNT(*) FROM NotificationPreferences").Returns<int>().FirstOrDefault();
        Console.WriteLine($"Created notification preferences for {prefsCount}/{userCount} users");
    }
    
    private void LogMigrationFailure(Exception ex)
    {
        Console.WriteLine($"Migration failed: {MIGRATION_PURPOSE}");
        Console.WriteLine($"Error: {ex.Message}");
    }
    
    private void LogRollbackStart()
    {
        Console.WriteLine($"Rolling back migration: {MIGRATION_PURPOSE}");
    }
    
    private void LogRollbackSuccess()
    {
        Console.WriteLine($"Rollback completed successfully: {MIGRATION_PURPOSE}");
    }
}
```

### 2. Version History Tracking

```csharp
[Migration(202401152000)]
public class EnhancedVersionTracking : Migration
{
    public override void Up()
    {
        // Enhance VersionInfo table with additional metadata
        if (Schema.Table("VersionInfo").Exists())
        {
            if (!Schema.Table("VersionInfo").Column("Description").Exists())
            {
                Alter.Table("VersionInfo")
                    .AddColumn("Description").AsString(500).Nullable()
                    .AddColumn("Author").AsString(100).Nullable()
                    .AddColumn("StoryReference").AsString(50).Nullable()
                    .AddColumn("ExecutionTime").AsInt32().Nullable()
                    .AddColumn("RollbackTested").AsBoolean().NotNullable().WithDefaultValue(false)
                    .AddColumn("ProductionApproved").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }
        
        // Create migration change log table
        Create.Table("MigrationChangeLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("MigrationVersion").AsInt64().NotNullable()
            .WithColumn("ChangeType").AsString(50).NotNullable() // CREATE, ALTER, DROP, INSERT, UPDATE, DELETE
            .WithColumn("ObjectType").AsString(50).NotNullable() // TABLE, COLUMN, INDEX, CONSTRAINT, etc.
            .WithColumn("ObjectName").AsString(200).NotNullable()
            .WithColumn("ChangeDetails").AsString(2000).Nullable()
            .WithColumn("ImpactAssessment").AsString(500).Nullable() // Performance, Breaking Change, etc.
            .WithColumn("RecordedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
            
        Create.Index("IX_MigrationChangeLog_Version")
            .OnTable("MigrationChangeLog")
            .OnColumn("MigrationVersion");
            
        Create.Index("IX_MigrationChangeLog_ChangeType")
            .OnTable("MigrationChangeLog")
            .OnColumn("ChangeType");
            
        // Record this migration's changes
        RecordMigrationChanges();
    }
    
    private void RecordMigrationChanges()
    {
        var migrationVersion = GetType().GetCustomAttribute<MigrationAttribute>()?.Version ?? 0;
        
        var changes = new[]
        {
            new { ChangeType = "ALTER", ObjectType = "TABLE", ObjectName = "VersionInfo", ChangeDetails = "Added metadata columns", ImpactAssessment = "Low - Non-breaking addition" },
            new { ChangeType = "CREATE", ObjectType = "TABLE", ObjectName = "MigrationChangeLog", ChangeDetails = "Created change tracking table", ImpactAssessment = "Low - New functionality" },
            new { ChangeType = "CREATE", ObjectType = "INDEX", ObjectName = "IX_MigrationChangeLog_Version", ChangeDetails = "Performance index", ImpactAssessment = "Positive - Improves query performance" },
            new { ChangeType = "CREATE", ObjectType = "INDEX", ObjectName = "IX_MigrationChangeLog_ChangeType", ChangeDetails = "Performance index", ImpactAssessment = "Positive - Improves query performance" }
        };
        
        foreach (var change in changes)
        {
            Insert.IntoTable("MigrationChangeLog")
                .Row(new
                {
                    MigrationVersion = migrationVersion,
                    ChangeType = change.ChangeType,
                    ObjectType = change.ObjectType,
                    ObjectName = change.ObjectName,
                    ChangeDetails = change.ChangeDetails,
                    ImpactAssessment = change.ImpactAssessment,
                    RecordedAt = DateTime.UtcNow
                });
        }
    }

    public override void Down()
    {
        Delete.Table("MigrationChangeLog");
        
        if (Schema.Table("VersionInfo").Column("Description").Exists())
        {
            Delete.Column("ProductionApproved").FromTable("VersionInfo");
            Delete.Column("RollbackTested").FromTable("VersionInfo");
            Delete.Column("ExecutionTime").FromTable("VersionInfo");
            Delete.Column("StoryReference").FromTable("VersionInfo");
            Delete.Column("Author").FromTable("VersionInfo");
            Delete.Column("Description").FromTable("VersionInfo");
        }
    }
}
```

### 3. Automated Version Validation

```csharp
public class MigrationVersionValidator
{
    private readonly IMigrationRunner _runner;
    private readonly ILogger<MigrationVersionValidator> _logger;
    
    public MigrationVersionValidator(IMigrationRunner runner, ILogger<MigrationVersionValidator> logger)
    {
        _runner = runner;
        _logger = logger;
    }
    
    public ValidationResult ValidateMigrationVersions(Assembly migrationAssembly)
    {
        var result = new ValidationResult();
        
        try
        {
            var migrations = GetMigrations(migrationAssembly);
            
            // Validate version format
            ValidateVersionFormat(migrations, result);
            
            // Check for duplicates
            ValidateNoDuplicateVersions(migrations, result);
            
            // Validate chronological order
            ValidateChronologicalOrder(migrations, result);
            
            // Check against applied migrations
            ValidateAgainstAppliedMigrations(migrations, result);
            
            // Validate dependencies
            ValidateMigrationDependencies(migrations, result);
            
            _logger.LogInformation($"Migration validation completed. Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Validation failed with exception: {ex.Message}");
            _logger.LogError(ex, "Migration validation failed");
        }
        
        return result;
    }
    
    private void ValidateVersionFormat(List<MigrationInfo> migrations, ValidationResult result)
    {
        foreach (var migration in migrations)
        {
            if (!IsValidTimestampVersion(migration.Version))
            {
                result.Warnings.Add($"Migration {migration.Name} has non-timestamp version {migration.Version}");
            }
        }
    }
    
    private void ValidateNoDuplicateVersions(List<MigrationInfo> migrations, ValidationResult result)
    {
        var duplicates = migrations
            .GroupBy(m => m.Version)
            .Where(g => g.Count() > 1)
            .ToList();
            
        foreach (var duplicate in duplicates)
        {
            var migrationNames = string.Join(", ", duplicate.Select(d => d.Name));
            result.Errors.Add($"Duplicate version {duplicate.Key} found in migrations: {migrationNames}");
        }
    }
    
    private void ValidateChronologicalOrder(List<MigrationInfo> migrations, ValidationResult result)
    {
        var sortedMigrations = migrations.OrderBy(m => m.Version).ToList();
        
        for (int i = 1; i < sortedMigrations.Count; i++)
        {
            var current = sortedMigrations[i];
            var previous = sortedMigrations[i - 1];
            
            if (IsValidTimestampVersion(current.Version) && IsValidTimestampVersion(previous.Version))
            {
                var currentTime = ParseTimestampVersion(current.Version);
                var previousTime = ParseTimestampVersion(previous.Version);
                
                if (currentTime <= previousTime)
                {
                    result.Warnings.Add($"Migration {current.Name} ({current.Version}) has timestamp before or same as previous migration {previous.Name} ({previous.Version})");
                }
            }
        }
    }
    
    private void ValidateAgainstAppliedMigrations(List<MigrationInfo> migrations, ValidationResult result)
    {
        var appliedVersions = GetAppliedMigrationVersions();
        var newMigrations = migrations.Where(m => !appliedVersions.Contains(m.Version)).ToList();
        
        foreach (var newMigration in newMigrations)
        {
            var higherAppliedVersions = appliedVersions.Where(v => v > newMigration.Version).ToList();
            if (higherAppliedVersions.Any())
            {
                result.Warnings.Add($"New migration {newMigration.Name} ({newMigration.Version}) is out of order. Higher versions already applied: {string.Join(", ", higherAppliedVersions)}");
            }
        }
    }
    
    private bool IsValidTimestampVersion(long version)
    {
        var versionStr = version.ToString();
        return versionStr.Length == 12 && 
               DateTime.TryParseExact(versionStr, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
    
    private DateTime ParseTimestampVersion(long version)
    {
        return DateTime.ParseExact(version.ToString(), "yyyyMMddHHmm", CultureInfo.InvariantCulture);
    }
    
    // Additional helper methods...
}

public class ValidationResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();
}

public class MigrationInfo
{
    public Type Type { get; set; }
    public long Version { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
}
```

## Best Practices Summary

### ✅ Do:
- Use timestamp-based versioning (YYYYMMDDHHNN) for team environments
- Leave gaps in numbering for emergency fixes
- Document migration purpose and changes
- Validate version conflicts before deployment
- Test rollback scenarios
- Track migration metadata and performance
- Use branch-specific versioning for complex workflows

### ❌ Don't:
- Use sequential numbering in team environments without coordination
- Skip version validation in CI/CD pipelines
- Apply migrations without understanding dependencies
- Ignore out-of-order migration warnings
- Deploy migrations without rollback testing
- Use random or arbitrary version numbers

Effective migration versioning ensures smooth database evolution and team collaboration while maintaining system reliability and traceability.

## See Also

- [Best Practices](best-practices.md)
- [Conditional Logic](conditional-logic.md)
- [FAQ & Troubleshooting](../faq.md)
- [Database Providers](../providers/)
- [Common Operations](../operations/)