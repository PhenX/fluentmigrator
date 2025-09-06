# Managing Columns

For comprehensive column management, data types, and modifiers, see the dedicated [Managing Columns](../managing-columns.md) guide.

This section covers only the most essential column operations in the context of other table operations.

## Basic Column Operations

### Adding Columns

```csharp
public class AddColumn : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("Email").FromTable("Users");
    }
}
```

### Modifying Columns

```csharp
public class ModifyColumn : Migration
{
    public override void Up()
    {
        Alter.Column("Name").OnTable("Users")
            .AsString(100).NotNullable();
    }

    public override void Down()
    {
        Alter.Column("Name").OnTable("Users")
            .AsString(50).Nullable();
    }
}
```

### Removing Columns

```csharp
public class RemoveColumn : Migration
{
    public override void Up()
    {
        Delete.Column("ObsoleteField").FromTable("Users");
    }

    public override void Down()
    {
        Alter.Table("Users")
            .AddColumn("ObsoleteField").AsString(100).Nullable();
    }
}
```

## See Also

- **[Managing Columns](../managing-columns.md)** - Comprehensive guide to column data types, modifiers, and best practices
- **[Working with Constraints](../working-with-constraints.md)** - Detailed constraint management
- **[Managing Indexes](../managing-indexes.md)** - Index optimization and management
- **[Working with Foreign Keys](../working-with-foreign-keys.md)** - Relationship management
- [Creating Tables](create-tables.md) - Complete table creation guide
- [Altering Tables](alter-tables.md) - Table modification operations