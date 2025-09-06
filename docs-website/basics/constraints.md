# Working with Constraints

Database constraints are essential for maintaining data integrity and enforcing business rules at the database level. FluentMigrator provides comprehensive support for creating and managing various types of constraints across different database providers.

## Overview

Constraints ensure data quality by:
- Preventing invalid data entry
- Maintaining referential integrity
- Enforcing business rules
- Providing automatic data validation

## Check Constraints

Check constraints validate data based on logical expressions.

### Basic Check Constraints

```csharp
public class BasicCheckConstraints : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Age").AsInt32().NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Rating").AsDecimal(3, 2).Nullable();

        // Age validation
        Create.CheckConstraint("CK_Users_Age")
            .OnTable("Users")
            .WithSql("Age >= 0 AND Age <= 150");

        // Rating validation
        Create.CheckConstraint("CK_Users_Rating")
            .OnTable("Users")
            .WithSql("Rating >= 0.0 AND Rating <= 5.0");
    }

    public override void Down()
    {
        Delete.CheckConstraint("CK_Users_Age").FromTable("Users");
        Delete.CheckConstraint("CK_Users_Rating").FromTable("Users");
        Delete.Table("Users");
    }
}
```

### Advanced Check Constraints

```csharp
public class AdvancedCheckConstraints : Migration
{
    public override void Up()
    {
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("ShipDate").AsDateTime().Nullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("Total").AsDecimal(10, 2).NotNullable();

        // Date validation
        Create.CheckConstraint("CK_Orders_ShipDate")
            .OnTable("Orders")
            .WithSql("ShipDate IS NULL OR ShipDate >= OrderDate");

        // Status validation
        Create.CheckConstraint("CK_Orders_Status")
            .OnTable("Orders")
            .WithSql("Status IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled')");

        // Total validation
        Create.CheckConstraint("CK_Orders_Total")
            .OnTable("Orders")
            .WithSql("Total > 0");
    }

    public override void Down()
    {
        Delete.CheckConstraint("CK_Orders_ShipDate").FromTable("Orders");
        Delete.CheckConstraint("CK_Orders_Status").FromTable("Orders");
        Delete.CheckConstraint("CK_Orders_Total").FromTable("Orders");
        Delete.Table("Orders");
    }
}
```

## Unique Constraints

Unique constraints ensure column values are unique across the table.

### Single Column Unique Constraints

```csharp
public class SingleColumnUnique : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable();

        // Username must be unique
        Create.UniqueConstraint("UQ_Users_Username")
            .OnTable("Users")
            .Column("Username");

        // Email must be unique
        Create.UniqueConstraint("UQ_Users_Email")
            .OnTable("Users")
            .Column("Email");
    }

    public override void Down()
    {
        Delete.UniqueConstraint("UQ_Users_Username").FromTable("Users");
        Delete.UniqueConstraint("UQ_Users_Email").FromTable("Users");
        Delete.Table("Users");
    }
}
```

### Composite Unique Constraints

```csharp
public class CompositeUnique : Migration
{
    public override void Up()
    {
        Create.Table("OrderItems")
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable();

        // Composite unique constraint - one product per order
        Create.UniqueConstraint("UQ_OrderItems_OrderId_ProductId")
            .OnTable("OrderItems")
            .Columns("OrderId", "ProductId");
    }

    public override void Down()
    {
        Delete.UniqueConstraint("UQ_OrderItems_OrderId_ProductId").FromTable("OrderItems");
        Delete.Table("OrderItems");
    }
}
```

## Default Constraints

Default constraints provide automatic values when no value is specified.

### Basic Default Values

```csharp
public class DefaultConstraints : Migration
{
    public override void Up()
    {
        Create.Table("Products")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("Quantity").AsInt32().NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Table("Products");
    }
}
```

## Database-Specific Constraints

### SQL Server Constraints

```csharp
public class SqlServerConstraints : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                ALTER TABLE Products
                ADD CONSTRAINT CK_Products_PriceQuantity
                CHECK (Price * Quantity >= 0)");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("ALTER TABLE Products DROP CONSTRAINT CK_Products_PriceQuantity");
    }
}
```

### PostgreSQL Constraints

```csharp
public class PostgreSqlConstraints : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Create.Table("Users")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Email").AsString(255).NotNullable()
                .WithColumn("Age").AsInt32().NotNullable();

            // PostgreSQL-specific check constraint
            Create.CheckConstraint("CK_Users_Email_Format")
                .OnTable("Users")
                .WithSql("Email ~ '^[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+[.][A-Za-z]+$'");
    });
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Delete.CheckConstraint("CK_Users_Email_Format").FromTable("Users");
            Delete.Table("Users");
    });
    }
}
```

## Managing Existing Constraints

### Adding Constraints to Existing Tables

```csharp
public class AddConstraintsToExistingTable : Migration
{
    public override void Up()
    {
        // Add check constraint
        Create.CheckConstraint("CK_Users_Age")
            .OnTable("Users")
            .WithSql("Age >= 18");

        // Add unique constraint
        Create.UniqueConstraint("UQ_Users_Email")
            .OnTable("Users")
            .Column("Email");
    }

    public override void Down()
    {
        Delete.CheckConstraint("CK_Users_Age").FromTable("Users");
        Delete.UniqueConstraint("UQ_Users_Email").FromTable("Users");
    }
}
```

### Removing Constraints

```csharp
public class RemoveConstraints : Migration
{
    public override void Up()
    {
        Delete.CheckConstraint("CK_Users_Age").FromTable("Users");
        Delete.UniqueConstraint("UQ_Users_Email").FromTable("Users");
    }

    public override void Down()
    {
        Create.CheckConstraint("CK_Users_Age")
            .OnTable("Users")
            .WithSql("Age >= 18");

        Create.UniqueConstraint("UQ_Users_Email")
            .OnTable("Users")
            .Column("Email");
    }
}
```

## Best Practices

### Naming Conventions
- Use descriptive constraint names with prefixes:
  - `CK_` for check constraints
  - `UQ_` for unique constraints
  - `DF_` for default constraints
- Include table name and column name(s) in constraint name

### Performance Considerations
- Check constraints are evaluated on every INSERT/UPDATE
- Complex check constraints can impact performance
- Consider indexing columns used in constraint expressions
- See [Indexes](/basics/indexes.md) for optimization strategies

### Error Handling
- Provide meaningful constraint names for better error messages
- Test constraint validation thoroughly
- Consider application-level validation alongside database constraints

### Cross-Database Compatibility
- Test constraints across all target database providers
- Use conditional logic for database-specific constraint syntax
- Some constraint features may not be supported on all providers

## Troubleshooting

### Common Issues
1. **Constraint Violation Errors**: Review existing data before adding constraints
2. **Performance Impact**: Monitor constraint evaluation performance
3. **Cross-Database Syntax**: Use `IfDatabase()` for provider-specific syntax
