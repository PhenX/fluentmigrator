---
layout: home

hero:
  name: FluentMigrator
  text: Database Schema Management
  tagline: A migration framework for .NET much like Ruby on Rails Migrations
  image:
    src: /logo-big.png
    alt: FluentMigrator Logo
  actions:
    - theme: brand
      text: Quick Start
      link: /guide/quick-start
    - theme: alt
      text: View on GitHub
      link: https://github.com/fluentmigrator/fluentmigrator

features:
  - icon: 🚀
    title: Easy to Use
    details: Write database migrations in C# using a fluent API that's easy to learn and understand.
  
  - icon: 🗃️
    title: Multi-Database Support  
    details: Supports SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Firebird, and more database providers.
  
  - icon: ⚡
    title: Version Control Friendly
    details: Migrations are code that can be checked into version control and shared across teams.
  
  - icon: 🔄
    title: Rollback Support
    details: Define both Up and Down methods to enable rolling back migrations when needed.
  
  - icon: 🎯
    title: Conditional Logic
    details: Use conditional logic to create database-specific migrations for different providers.
  
  - icon: 🛠️
    title: Extensible
    details: Extensible architecture with support for custom extensions and database-specific features.
---

## What is FluentMigrator?

FluentMigrator is a migration framework for .NET that allows you to manage database schema changes in a structured, version-controlled way. Instead of manually running SQL scripts, you write migrations as C# classes that can be executed automatically.

```csharp
[Migration(20240101000000)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable().Unique()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

## Key Benefits

- **Database Agnostic**: Write once, run on multiple database providers
- **Type Safe**: Catch errors at compile time instead of runtime
- **Team Friendly**: Share schema changes through version control
- **Automated**: Integrate with CI/CD pipelines for automatic deployments

## Get Started

Ready to start using FluentMigrator? Check out our [Quick Start Guide](/guide/quick-start) to create your first migration in just a few minutes.

## Community & Support

- **GitHub**: [fluentmigrator/fluentmigrator](https://github.com/fluentmigrator/fluentmigrator)
- **Stack Overflow**: Use the [`fluent-migrator`](https://stackoverflow.com/questions/tagged/fluent-migrator) tag
- **Discussions**: [GitHub Discussions](https://github.com/fluentmigrator/fluentmigrator/discussions)
- **Issues**: [Bug Reports & Feature Requests](https://github.com/fluentmigrator/fluentmigrator/issues)