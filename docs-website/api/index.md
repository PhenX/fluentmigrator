# API Reference

This section provides detailed information about FluentMigrator's core API classes and methods.

## Migration Base Class

All migrations inherit from the `Migration` base class, which provides the core functionality for defining database schema changes.

### Migration Class

```csharp
public abstract class Migration
{
    public abstract void Up();
    public virtual void Down() { }
    
    // Properties
    public IServiceProvider ApplicationContext { get; }
    public IMigrationContext Context { get; }
}
```

#### Methods

- **Up()**: Define the changes to apply when migrating forward
- **Down()**: Define how to rollback the changes (optional but recommended)

#### Attributes

- **[Migration(version)]**: Marks a class as a migration with a specific version number

### Example Migration

```csharp
[Migration(20240101120000)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

## Expression Builders

FluentMigrator uses a fluent API built on expression builders that provide a readable way to define database operations.

### Create Builder

The `Create` builder is used to create new database objects.

#### Tables
```csharp
Create.Table("TableName")
    .WithColumn("ColumnName").AsDataType().Constraints()
```

#### Indexes
```csharp
Create.Index("IndexName").OnTable("TableName")
    .OnColumn("ColumnName").Ascending()
```

#### Foreign Keys
```csharp
Create.ForeignKey("FK_Name")
    .FromTable("ChildTable").ForeignColumn("ForeignKeyColumn")
    .ToTable("ParentTable").PrimaryColumn("PrimaryKeyColumn")
```

### Alter Builder

The `Alter` builder is used to modify existing database objects.

#### Add Columns
```csharp
Alter.Table("TableName")
    .AddColumn("NewColumn").AsDataType().Constraints()
```

#### Modify Columns
```csharp
Alter.Column("ColumnName").OnTable("TableName")
    .AsDataType().Constraints()
```

### Delete Builder

The `Delete` builder is used to remove database objects.

#### Tables
```csharp
Delete.Table("TableName")
```

#### Columns
```csharp
Delete.Column("ColumnName").FromTable("TableName")
```

#### Constraints
```csharp
Delete.ForeignKey("FK_Name").OnTable("TableName")
```

For complete API documentation, visit the [official FluentMigrator documentation](https://fluentmigrator.github.io).