# Quick Start Guide

Get up and running with FluentMigrator in just a few minutes. This guide will walk you through creating your first migration and running it against a database.

## Prerequisites

- .NET 6.0 or later
- A supported database (SQL Server, PostgreSQL, MySQL, SQLite, etc.)
- Basic knowledge of C# and database concepts

## Step 1: Install FluentMigrator

### Using Package Manager Console
```powershell
Install-Package FluentMigrator
Install-Package FluentMigrator.Runner
```

### Using .NET CLI
```bash
dotnet add package FluentMigrator
dotnet add package FluentMigrator.Runner
```

### Using PackageReference
Add these to your `.csproj` file:
```xml
<PackageReference Include="FluentMigrator" Version="6.2.0" />
<PackageReference Include="FluentMigrator.Runner" Version="6.2.0" />
```

You'll also need a database provider package:
```xml
<!-- For SQL Server -->
<PackageReference Include="FluentMigrator.Runner.SqlServer" Version="6.2.0" />

<!-- For PostgreSQL -->
<PackageReference Include="FluentMigrator.Runner.Postgres" Version="6.2.0" />

<!-- For MySQL -->
<PackageReference Include="FluentMigrator.Runner.MySql" Version="6.2.0" />

<!-- For SQLite -->
<PackageReference Include="FluentMigrator.Runner.SQLite" Version="6.2.0" />
```

## Step 2: Create Your First Migration

Create a new class that inherits from `Migration`:

```csharp
using FluentMigrator;

[Migration(20240101120000)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable().Unique()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("FirstName").AsString(100).Nullable()
            .WithColumn("LastName").AsString(100).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

### Key Points:
- **Migration Attribute**: `[Migration(20240101120000)]` - Use a timestamp format (YYYYMMDDhhmmss)
- **Up Method**: Defines changes to apply when migrating forward
- **Down Method**: Defines how to undo the changes (for rollbacks)

## Step 3: Configure the Migration Runner

Create a console application or add migration support to your existing app:

```csharp
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        var serviceProvider = CreateServices();

        // Put the database update into a scope to ensure
        // that all resources will be disposed.
        using (var scope = serviceProvider.CreateScope())
        {
            UpdateDatabase(scope.ServiceProvider);
        }
    }

    /// <summary>
    /// Configure the dependency injection services
    /// </summary>
    private static ServiceProvider CreateServices()
    {
        return new ServiceCollection()
            // Add common FluentMigrator services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                // Add SQL Server support to FluentMigrator
                .AddSqlServer()
                // Set the connection string
                .WithGlobalConnectionString("Server=.;Database=MyApp;Trusted_Connection=true;")
                // Define the assembly containing the migrations
                .ScanIn(typeof(CreateUsersTable).Assembly).For.Migrations())
            // Enable logging to console in the FluentMigrator way
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            // Build the service provider
            .BuildServiceProvider(false);
    }

    /// <summary>
    /// Update the database
    /// </summary>
    private static void UpdateDatabase(IServiceProvider serviceProvider)
    {
        // Instantiate the runner
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

        // Execute the migrations
        runner.MigrateUp();
    }
}
```

## Step 4: Run Your Migration

Run your application and FluentMigrator will:
1. Create a version tracking table (usually named `VersionInfo`)
2. Execute your migration
3. Mark the migration as completed in the tracking table

## Step 5: Add More Migrations

As your application evolves, add more migrations:

```csharp
[Migration(20240102090000)]
public class AddUserRoles : Migration
{
    public override void Up()
    {
        Create.Table("Roles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable().Unique()
            .WithColumn("Description").AsString(255).Nullable();

        Insert.IntoTable("Roles")
            .Row(new { Name = "Admin", Description = "Administrator" })
            .Row(new { Name = "User", Description = "Regular User" });

        Alter.Table("Users")
            .AddColumn("RoleId").AsInt32().Nullable()
                .ForeignKey("FK_Users_Roles", "Roles", "Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Users_Roles").OnTable("Users");
        Delete.Column("RoleId").FromTable("Users");
        Delete.Table("Roles");
    }
}
```

## Alternative Database Configurations

### PostgreSQL
```csharp
.AddPostgres()
.WithGlobalConnectionString("Host=localhost;Database=myapp;Username=myuser;Password=mypass")
```

### MySQL
```csharp
.AddMySql5()
.WithGlobalConnectionString("Server=localhost;Database=myapp;Uid=myuser;Pwd=mypass;")
```

### SQLite
```csharp
.AddSQLite()
.WithGlobalConnectionString("Data Source=myapp.db")
```

## Using appsettings.json

For real applications, store connection strings in configuration:

```json
{
  "ConnectionStrings": {
    "Default": "Server=.;Database=MyApp;Trusted_Connection=true;"
  }
}
```

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var connectionString = configuration.GetConnectionString("Default");

return new ServiceCollection()
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSqlServer()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(CreateUsersTable).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole())
    .BuildServiceProvider(false);
```

## Next Steps

Now that you have your first migration running, explore these topics:

- [Creating Tables](./operations/create-tables.md) - Learn all the options for creating tables
- [Altering Tables](./operations/alter-tables.md) - Modify existing tables safely
- [Database Providers](./providers/sql-server.md) - Provider-specific features and considerations
- [Best Practices](./advanced/best-practices.md) - Tips for writing maintainable migrations

## Common Issues

### Migration Not Found
- Ensure your migration class is public
- Check that the assembly containing migrations is being scanned
- Verify the migration attribute has a unique version number

### Connection String Issues
- Test your connection string with a simple database connection
- Ensure the database exists or can be created
- Check permissions for the database user

### Multiple Databases
If you need to support multiple database providers, see our [Database Providers](./providers/) section for provider-specific considerations.