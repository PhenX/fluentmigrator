# Basic Examples

This collection of examples demonstrates common FluentMigrator usage patterns and scenarios.

## Table Management

### Creating a Simple User Table
```csharp
[Migration(20240101120000)]
public class CreateUserTable : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable().Unique()
            .WithColumn("Email").AsString(255).NotNullable().Unique()
            .WithColumn("FirstName").AsString(100).Nullable()
            .WithColumn("LastName").AsString(100).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable();
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}
```

### Adding Columns to Existing Table
```csharp
[Migration(20240101130000)]
public class AddPhoneNumberToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("PhoneNumber").AsString(20).Nullable();
    }

    public override void Down()
    {
        Delete.Column("PhoneNumber").FromTable("Users");
    }
}
```

### Modifying Column Properties
```csharp
[Migration(20240101140000)]
public class MakePhoneNumberRequired : Migration
{
    public override void Up()
    {
        // First, update existing NULL values
        Execute.Sql("UPDATE Users SET PhoneNumber = '' WHERE PhoneNumber IS NULL");
        
        // Then make the column NOT NULL
        Alter.Column("PhoneNumber").OnTable("Users")
            .AsString(20).NotNullable();
    }

    public override void Down()
    {
        Alter.Column("PhoneNumber").OnTable("Users")
            .AsString(20).Nullable();
    }
}
```

## Relationships and Constraints

### Creating Related Tables
```csharp
[Migration(20240101150000)]
public class CreateCategoriesAndProducts : Migration
{
    public override void Up()
    {
        // Create Categories table first
        Create.Table("Categories")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

        // Create Products table with foreign key to Categories
        Create.Table("Products")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("Description").AsString().Nullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable()
            .WithColumn("CategoryId").AsInt32().NotNullable()
                .ForeignKey("FK_Products_Categories", "Categories", "Id")
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // Create index on foreign key for better performance
        Create.Index("IX_Products_CategoryId").OnTable("Products").OnColumn("CategoryId");
    }

    public override void Down()
    {
        Delete.Table("Products");
        Delete.Table("Categories");
    }
}
```

### Many-to-Many Relationship
```csharp
[Migration(20240101160000)]
public class CreateUserRoles : Migration
{
    public override void Up()
    {
        // Create Roles table
        Create.Table("Roles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable().Unique()
            .WithColumn("Description").AsString(255).Nullable();

        // Create junction table for many-to-many relationship
        Create.Table("UserRoles")
            .WithColumn("UserId").AsInt32().NotNullable()
                .ForeignKey("FK_UserRoles_Users", "Users", "Id")
                .OnDelete(Rule.Cascade)
            .WithColumn("RoleId").AsInt32().NotNullable()
                .ForeignKey("FK_UserRoles_Roles", "Roles", "Id")
                .OnDelete(Rule.Cascade)
            .WithColumn("AssignedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("AssignedBy").AsInt32().NotNullable()
                .ForeignKey("FK_UserRoles_AssignedByUser", "Users", "Id");

        // Composite primary key
        Create.PrimaryKey("PK_UserRoles")
            .OnTable("UserRoles")
            .Columns("UserId", "RoleId");

        // Insert default roles
        Insert.IntoTable("Roles")
            .Row(new { Name = "Admin", Description = "Administrator with full access" })
            .Row(new { Name = "User", Description = "Regular user with limited access" })
            .Row(new { Name = "Moderator", Description = "Moderator with content management access" });
    }

    public override void Down()
    {
        Delete.Table("UserRoles");
        Delete.Table("Roles");
    }
}
```

## Data Operations

### Inserting Seed Data
```csharp
[Migration(20240101170000)]
public class InsertInitialData : Migration
{
    public override void Up()
    {
        // Insert categories
        Insert.IntoTable("Categories")
            .Row(new { Name = "Electronics", Description = "Electronic devices and accessories" })
            .Row(new { Name = "Clothing", Description = "Apparel and fashion items" })
            .Row(new { Name = "Books", Description = "Books and publications" })
            .Row(new { Name = "Sports", Description = "Sports equipment and accessories" });

        // Insert sample products
        Insert.IntoTable("Products")
            .Row(new { 
                Name = "Laptop Computer", 
                Description = "High-performance laptop for work and gaming",
                Price = 1299.99m,
                CategoryId = 1 
            })
            .Row(new { 
                Name = "Running Shoes", 
                Description = "Comfortable running shoes for daily exercise",
                Price = 89.99m,
                CategoryId = 4 
            })
            .Row(new { 
                Name = "Programming Book", 
                Description = "Comprehensive guide to software development",
                Price = 45.00m,
                CategoryId = 3 
            });
    }

    public override void Down()
    {
        Delete.FromTable("Products").AllRows();
        Delete.FromTable("Categories").AllRows();
    }
}
```

### Updating Existing Data
```csharp
[Migration(20240101180000)]
public class UpdateProductPrices : Migration
{
    public override void Up()
    {
        // Increase all product prices by 5%
        Execute.Sql("UPDATE Products SET Price = Price * 1.05");
        
        // Update specific product
        Update.Table("Products")
            .Set(new { Description = "Updated: High-performance laptop for work and gaming" })
            .Where(new { Name = "Laptop Computer" });
    }

    public override void Down()
    {
        // Revert price changes
        Execute.Sql("UPDATE Products SET Price = Price / 1.05");
        
        // Revert description change
        Update.Table("Products")
            .Set(new { Description = "High-performance laptop for work and gaming" })
            .Where(new { Name = "Laptop Computer" });
    }
}
```

## Indexes and Performance

### Creating Various Index Types
```csharp
[Migration(20240101190000)]
public class CreateIndexes : Migration
{
    public override void Up()
    {
        // Simple index on frequently searched column
        Create.Index("IX_Users_Email").OnTable("Users")
            .OnColumn("Email").Ascending();

        // Composite index for complex queries
        Create.Index("IX_Products_CategoryPrice").OnTable("Products")
            .OnColumn("CategoryId").Ascending()
            .OnColumn("Price").Descending();

        // Unique index to enforce business rules
        Create.Index("IX_Products_Name_Unique").OnTable("Products")
            .OnColumn("Name").Ascending()
            .WithOptions().Unique();

        // Index on computed expression (database-specific)
        IfDatabase("SqlServer")
            .Create.Index("IX_Users_FullName").OnTable("Users")
            .OnColumn(RawSql.Insert("FirstName + ' ' + LastName"));

        IfDatabase("Postgres")
            .Create.Index("IX_Users_FullName").OnTable("Users")
            .OnColumn(RawSql.Insert("FirstName || ' ' || LastName"));
    }

    public override void Down()
    {
        Delete.Index("IX_Users_Email").OnTable("Users");
        Delete.Index("IX_Products_CategoryPrice").OnTable("Products");
        Delete.Index("IX_Products_Name_Unique").OnTable("Products");
        
        IfDatabase("SqlServer", "Postgres")
            .Delete.Index("IX_Users_FullName").OnTable("Users");
    }
}
```

## Schema Organization

### Using Schemas for Organization
```csharp
[Migration(20240101200000)]
public class CreateSchemasAndTables : Migration
{
    public override void Up()
    {
        // Create schemas for different domains
        Create.Schema("Sales");
        Create.Schema("Inventory");
        Create.Schema("HR");

        // Create tables in different schemas
        Create.Table("Orders").InSchema("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderNumber").AsString(20).NotNullable().Unique()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable();

        Create.Table("Inventory").InSchema("Inventory")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("ProductId").AsInt32().NotNullable()
                .ForeignKey("FK_Inventory_Products", "dbo", "Products", "Id")
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("Location").AsString(50).NotNullable()
            .WithColumn("LastUpdated").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        Create.Table("Employees").InSchema("HR")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("EmployeeNumber").AsString(10).NotNullable().Unique()
            .WithColumn("FirstName").AsString(50).NotNullable()
            .WithColumn("LastName").AsString(50).NotNullable()
            .WithColumn("Department").AsString(50).NotNullable()
            .WithColumn("HireDate").AsDate().NotNullable()
            .WithColumn("Salary").AsDecimal(10, 2).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Employees").InSchema("HR");
        Delete.Table("Inventory").InSchema("Inventory");
        Delete.Table("Orders").InSchema("Sales");
        
        Delete.Schema("HR");
        Delete.Schema("Inventory");
        Delete.Schema("Sales");
    }
}
```

## Database-Specific Features

### Conditional Migration Based on Database Provider
```csharp
[Migration(20240101210000)]
public class DatabaseSpecificFeatures : Migration
{
    public override void Up()
    {
        // Common table structure
        Create.Table("Documents")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(255).NotNullable()
            .WithColumn("Content").AsString().NotNullable();

        // SQL Server specific features
        IfDatabase("SqlServer").Execute.Sql(@"
            ALTER TABLE Documents 
            ADD SearchContent AS (Title + ' ' + Content);
            
            CREATE FULLTEXT CATALOG DocumentCatalog;
            CREATE FULLTEXT INDEX ON Documents (Title, Content) 
            KEY INDEX PK_Documents;
        ");

        // PostgreSQL specific features
        IfDatabase("Postgres").Execute.Sql(@"
            ALTER TABLE Documents 
            ADD COLUMN search_vector tsvector;
            
            CREATE INDEX IX_Documents_SearchVector 
            ON Documents USING gin(search_vector);
            
            CREATE TRIGGER update_document_search_vector
            BEFORE INSERT OR UPDATE ON Documents
            FOR EACH ROW EXECUTE FUNCTION 
            tsvector_update_trigger(search_vector, 'pg_catalog.english', title, content);
        ");

        // MySQL specific features
        IfDatabase("MySql").Execute.Sql(@"
            ALTER TABLE Documents 
            ENGINE=InnoDB 
            DEFAULT CHARSET=utf8mb4 
            COLLATE=utf8mb4_unicode_ci;
            
            CREATE FULLTEXT INDEX FTI_Documents_Content 
            ON Documents (Title, Content);
        ");
    }

    public override void Down()
    {
        Delete.Table("Documents");
    }
}
```

## Common Patterns

### Audit Trail Pattern
```csharp
[Migration(20240101220000)]
public class CreateAuditTrail : Migration
{
    public override void Up()
    {
        // Create audit log table
        Create.Table("AuditLog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TableName").AsString(100).NotNullable()
            .WithColumn("RecordId").AsInt32().NotNullable()
            .WithColumn("Action").AsString(10).NotNullable() // INSERT, UPDATE, DELETE
            .WithColumn("OldValues").AsString().Nullable()    // JSON string of old values
            .WithColumn("NewValues").AsString().Nullable()    // JSON string of new values
            .WithColumn("UserId").AsInt32().Nullable()
                .ForeignKey("FK_AuditLog_Users", "Users", "Id")
            .WithColumn("Timestamp").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // Create index for efficient querying
        Create.Index("IX_AuditLog_TableRecord").OnTable("AuditLog")
            .OnColumn("TableName").Ascending()
            .OnColumn("RecordId").Ascending()
            .OnColumn("Timestamp").Descending();
    }

    public override void Down()
    {
        Delete.Table("AuditLog");
    }
}
```

### Lookup Table Pattern
```csharp
[Migration(20240101230000)]
public class CreateLookupTables : Migration
{
    public override void Up()
    {
        // Create status lookup table
        Create.Table("OrderStatuses")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("Name").AsString(50).NotNullable().Unique()
            .WithColumn("Description").AsString(255).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("SortOrder").AsInt32().NotNullable();

        // Insert lookup values
        Insert.IntoTable("OrderStatuses")
            .Row(new { Id = 1, Name = "Pending", Description = "Order is pending processing", SortOrder = 1 })
            .Row(new { Id = 2, Name = "Processing", Description = "Order is being processed", SortOrder = 2 })
            .Row(new { Id = 3, Name = "Shipped", Description = "Order has been shipped", SortOrder = 3 })
            .Row(new { Id = 4, Name = "Delivered", Description = "Order has been delivered", SortOrder = 4 })
            .Row(new { Id = 5, Name = "Cancelled", Description = "Order has been cancelled", SortOrder = 5 });

        // Add status column to Orders table
        Alter.Table("Orders").InSchema("Sales")
            .AddColumn("StatusId").AsInt32().NotNullable().WithDefaultValue(1)
                .ForeignKey("FK_Orders_OrderStatuses", "dbo", "OrderStatuses", "Id");

        // Create index on status for filtering
        Create.Index("IX_Orders_StatusId").OnTable("Orders").InSchema("Sales")
            .OnColumn("StatusId");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Orders_OrderStatuses").OnTable("Orders").InSchema("Sales");
        Delete.Column("StatusId").FromTable("Orders").InSchema("Sales");
        Delete.Table("OrderStatuses");
    }
}
```

These examples demonstrate the most common FluentMigrator patterns you'll use in real-world applications. Each example includes both Up and Down methods to ensure migrations can be properly rolled back when needed.