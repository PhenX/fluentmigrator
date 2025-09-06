# Examples Overview

This section contains practical examples and real-world scenarios for using FluentMigrator effectively.

## Available Examples

### [Basic Examples](./basic.md)
Common patterns and fundamental operations:
- Creating tables and columns
- Managing relationships and constraints
- Data seeding and updates
- Index creation and optimization
- Schema organization

### [Advanced Scenarios](./advanced.md)
Complex use cases and advanced techniques:
- Large-scale data migrations
- Cross-database compatibility
- Performance optimization
- Custom extensions and providers
- Production deployment strategies

### [Real-world Use Cases](./real-world.md)
Practical examples from actual applications:
- E-commerce database design
- Multi-tenant applications
- Microservices data management
- Legacy system migrations
- Continuous deployment patterns

## Quick Reference

### Most Common Operations

```csharp
// Create a table
Create.Table("Users")
    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
    .WithColumn("Name").AsString(100).NotNullable();

// Add a column
Alter.Table("Users")
    .AddColumn("Email").AsString(255).Nullable();

// Create an index
Create.Index("IX_Users_Email").OnTable("Users")
    .OnColumn("Email");

// Insert data
Insert.IntoTable("Users")
    .Row(new { Name = "John Doe", Email = "john@example.com" });
```

### Database-Specific Examples

```csharp
// SQL Server specific
IfDatabase(ProcessorIdConstants.SqlServer)
    .Create.Index("IX_Users_Name").OnTable("Users")
    .OnColumn("Name")
    .Include("Email");

// PostgreSQL specific  
IfDatabase(ProcessorIdConstants.Postgres)
    .Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");

// MySQL specific
IfDatabase(ProcessorIdConstants.MySql)
    .Execute.Sql("ALTER TABLE Users ENGINE=InnoDB");
```

## Getting Started

If you're new to FluentMigrator, start with the [Basic Examples](./basic.md) to learn fundamental concepts, then progress to more advanced scenarios as needed.

For specific database providers, check the [Provider Documentation](../guide/providers/sql-server.md) for detailed information about database-specific features and optimizations.