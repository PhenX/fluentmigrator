# What is FluentMigrator?

FluentMigrator is a migration framework for .NET that provides a structured way to evolve your database schema over time. It allows you to describe database schema changes in C# code using a fluent API, which can then be executed against any supported database provider.

## The Problem

Traditional database development often involves:
- Manual SQL scripts that need to be run by each developer
- No clear versioning of database changes
- Risk of inconsistencies between development, testing, and production environments
- Difficulty tracking what changes have been applied
- Database-specific SQL that doesn't work across different providers

## The Solution

FluentMigrator solves these problems by:
- **Code-based migrations**: Database changes are written in C# using a fluent API
- **Version control**: Migrations are part of your codebase and can be checked into source control
- **Automatic tracking**: FluentMigrator keeps track of which migrations have been applied
- **Database agnostic**: Write once, run on multiple database providers
- **Rollback support**: Define both Up and Down methods for reversible migrations

## Core Concepts

### Migration Class

A migration is a class that inherits from `Migration` and implements `Up()` and `Down()` methods:

```csharp
[Migration(20240101120000)]
public class AddUserTable : Migration
{
    public override void Up()
    {
        // Define what happens when migrating forward
        Create.Table("Users")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable();
    }

    public override void Down()
    {
        // Define what happens when rolling back
        Delete.Table("Users");
    }
}
```

### Migration Attributes

- `[Migration(version)]`: Marks a class as a migration with a specific version number
- Version numbers determine the order of execution
- Common convention: `YYYYMMDDhhmmss` (timestamp format)

### Fluent API

FluentMigrator uses a fluent API that reads like natural language:

```csharp
Create.Table("Products")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(255).NotNullable()
    .WithColumn("Price").AsDecimal(10, 2).Nullable()
    .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
```

## Benefits

### Version Control Integration
- Migrations are C# code files that integrate naturally with version control
- Easy to see what changed and when
- Collaborative development without merge conflicts
- Branching and merging of schema changes

### Database Provider Independence
FluentMigrator supports multiple database providers:
- SQL Server
- PostgreSQL
- MySQL / MariaDB
- SQLite
- Oracle
- Firebird
- And more...

Write your migrations once and run them on any supported database.

### Team Development
- All team members can apply the same schema changes
- New developers can get up to date with a single command
- No more "Did you remember to run this script?" conversations

### CI/CD Integration
- Migrations can be automatically applied as part of deployment
- Consistent schema updates across all environments
- Reduced deployment errors

## When to Use FluentMigrator

FluentMigrator is ideal for:
- Applications with evolving database schemas
- Teams with multiple developers
- Projects that need to support multiple database providers
- Applications requiring automated deployments
- Projects where database schema needs to be version controlled

## Getting Started

Ready to create your first migration? Head over to the [Quick Start Guide](./quick-start.md) to learn how to set up FluentMigrator and create your first migration.