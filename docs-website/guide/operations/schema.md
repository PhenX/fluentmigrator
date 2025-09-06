# Schema Operations

FluentMigrator provides comprehensive support for database schema management, including creating schemas, managing permissions, and organizing database objects across different namespaces.

## Basic Schema Operations

### Creating and Managing Schemas

```csharp
public class BasicSchemaOperations : Migration
{
    public override void Up()
    {
        // Create schemas
        Create.Schema("Sales");
        Create.Schema("Inventory");
        Create.Schema("HR");
        Create.Schema("Reporting");
        
        // Create tables in specific schemas
        Create.Table("Customers")
            .InSchema("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        Create.Table("Products")
            .InSchema("Inventory")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("SKU").AsString(50).NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable();
            
        Create.Table("Employees")
            .InSchema("HR")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FirstName").AsString(50).NotNullable()
            .WithColumn("LastName").AsString(50).NotNullable()
            .WithColumn("Department").AsString(50).NotNullable()
            .WithColumn("HireDate").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Employees").InSchema("HR");
        Delete.Table("Products").InSchema("Inventory");
        Delete.Table("Customers").InSchema("Sales");
        
        Delete.Schema("Reporting");
        Delete.Schema("HR");
        Delete.Schema("Inventory");
        Delete.Schema("Sales");
    }
}
```

### Cross-Schema References

```csharp
public class CrossSchemaReferences : Migration
{
    public override void Up()
    {
        // Create related tables in different schemas
        Create.Table("Orders")
            .InSchema("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable();
            
        Create.Table("OrderItems")
            .InSchema("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable();
            
        // Cross-schema foreign keys
        Create.ForeignKey("FK_Orders_Customers")
            .FromTable("Orders").InSchema("Sales").ForeignColumn("CustomerId")
            .ToTable("Customers").InSchema("Sales").PrimaryColumn("Id");
            
        Create.ForeignKey("FK_OrderItems_Orders")
            .FromTable("OrderItems").InSchema("Sales").ForeignColumn("OrderId")
            .ToTable("Orders").InSchema("Sales").PrimaryColumn("Id");
            
        Create.ForeignKey("FK_OrderItems_Products")
            .FromTable("OrderItems").InSchema("Sales").ForeignColumn("ProductId")
            .ToTable("Products").InSchema("Inventory").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_OrderItems_Products").OnTable("OrderItems").InSchema("Sales");
        Delete.ForeignKey("FK_OrderItems_Orders").OnTable("OrderItems").InSchema("Sales");
        Delete.ForeignKey("FK_Orders_Customers").OnTable("Orders").InSchema("Sales");
        
        Delete.Table("OrderItems").InSchema("Sales");
        Delete.Table("Orders").InSchema("Sales");
    }
}
```

## Advanced Schema Organization

### Domain-Driven Schema Design

```csharp
public class DomainDrivenSchemas : Migration
{
    public override void Up()
    {
        // Create domain-specific schemas
        Create.Schema("CustomerManagement");
        Create.Schema("OrderProcessing");
        Create.Schema("ProductCatalog");
        Create.Schema("Accounting");
        Create.Schema("Shipping");
        
        // Customer Management domain
        Create.Table("Customers")
            .InSchema("CustomerManagement")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CompanyName").AsString(200).NotNullable()
            .WithColumn("ContactName").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Phone").AsString(20).Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
            
        Create.Table("CustomerAddresses")
            .InSchema("CustomerManagement")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("AddressType").AsString(20).NotNullable()
            .WithColumn("Street").AsString(200).NotNullable()
            .WithColumn("City").AsString(100).NotNullable()
            .WithColumn("State").AsString(50).NotNullable()
            .WithColumn("ZipCode").AsString(10).NotNullable()
            .WithColumn("Country").AsString(50).NotNullable();
            
        // Product Catalog domain
        Create.Table("Categories")
            .InSchema("ProductCatalog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("ParentCategoryId").AsInt32().Nullable();
            
        Create.Table("Products")
            .InSchema("ProductCatalog")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("SKU").AsString(50).NotNullable()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("UnitsInStock").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
            
        // Order Processing domain
        Create.Table("Orders")
            .InSchema("OrderProcessing")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderNumber").AsString(20).NotNullable()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("RequiredDate").AsDateTime().Nullable()
            .WithColumn("ShippedDate").AsDateTime().Nullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("SubTotal").AsDecimal(10, 2).NotNullable()
            .WithColumn("TaxAmount").AsDecimal(10, 2).NotNullable()
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable();
            
        Create.Table("OrderDetails")
            .InSchema("OrderProcessing")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("Discount").AsDecimal(5, 4).NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Table("OrderDetails").InSchema("OrderProcessing");
        Delete.Table("Orders").InSchema("OrderProcessing");
        Delete.Table("Products").InSchema("ProductCatalog");
        Delete.Table("Categories").InSchema("ProductCatalog");
        Delete.Table("CustomerAddresses").InSchema("CustomerManagement");
        Delete.Table("Customers").InSchema("CustomerManagement");
        
        Delete.Schema("Shipping");
        Delete.Schema("Accounting");
        Delete.Schema("ProductCatalog");
        Delete.Schema("OrderProcessing");
        Delete.Schema("CustomerManagement");
    }
}
```

### Environment-Based Schema Organization

```csharp
public class EnvironmentBasedSchemas : Migration
{
    public override void Up()
    {
        // Create environment-specific schemas for multi-tenant applications
        var environments = new[] { "Development", "Staging", "Production" };
        var tenants = new[] { "TenantA", "TenantB", "TenantC" };
        
        foreach (var env in environments)
        {
            foreach (var tenant in tenants)
            {
                var schemaName = $"{env}_{tenant}";
                Create.Schema(schemaName);
                
                // Create tenant-specific tables
                Create.Table("Users")
                    .InSchema(schemaName)
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("Username").AsString(50).NotNullable()
                    .WithColumn("Email").AsString(255).NotNullable()
                    .WithColumn("CreatedAt").AsDateTime().NotNullable();
                    
                Create.Table("Settings")
                    .InSchema(schemaName)
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("Key").AsString(100).NotNullable()
                    .WithColumn("Value").AsString(1000).Nullable()
                    .WithColumn("Category").AsString(50).NotNullable();
            }
        }
        
        // Create shared/common schema for reference data
        Create.Schema("Shared");
        
        Create.Table("Countries")
            .InSchema("Shared")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Code").AsString(3).NotNullable()
            .WithColumn("Name").AsString(100).NotNullable();
            
        Create.Table("Currencies")
            .InSchema("Shared")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Code").AsString(3).NotNullable()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Symbol").AsString(5).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Currencies").InSchema("Shared");
        Delete.Table("Countries").InSchema("Shared");
        Delete.Schema("Shared");
        
        var environments = new[] { "Development", "Staging", "Production" };
        var tenants = new[] { "TenantA", "TenantB", "TenantC" };
        
        foreach (var env in environments)
        {
            foreach (var tenant in tenants)
            {
                var schemaName = $"{env}_{tenant}";
                Delete.Table("Settings").InSchema(schemaName);
                Delete.Table("Users").InSchema(schemaName);
                Delete.Schema(schemaName);
            }
        }
    }
}
```

## Database-Specific Schema Features

### SQL Server Schema Security

```csharp
public class SqlServerSchemaFeatures : Migration
{
    public override void Up()
    {
        if (IfDatabase("SqlServer"))
        {
            // Create application-specific schemas with ownership
            Create.Schema("Application");
            Create.Schema("Security");
            Create.Schema("Audit");
            
            // Basic schema creation with Execute.Sql - for advanced examples see Raw SQL guide
            Execute.Sql("CREATE SCHEMA [Sales_ReadOnly]");
            Execute.Sql("CREATE SCHEMA [Sales_ReadWrite]");
            
            // Create tables in security-focused schemas
            Create.Table("Users")
                .InSchema("Security")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Username").AsString(50).NotNullable()
                .WithColumn("PasswordHash").AsString(255).NotNullable()
                .WithColumn("Salt").AsString(50).NotNullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable();
                
            Create.Table("UserRoles")
                .InSchema("Security")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("RoleName").AsString(50).NotNullable()
                .WithColumn("GrantedAt").AsDateTime().NotNullable()
                .WithColumn("GrantedBy").AsInt32().NotNullable();
                
            Create.Table("AuditLog")
                .InSchema("Audit")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("TableName").AsString(100).NotNullable()
                .WithColumn("Operation").AsString(20).NotNullable()
                .WithColumn("OldValues").AsCustom("NVARCHAR(MAX)").Nullable()
                .WithColumn("NewValues").AsCustom("NVARCHAR(MAX)").Nullable()
                .WithColumn("ChangedBy").AsInt32().NotNullable()
                .WithColumn("ChangedAt").AsDateTime().NotNullable();
                
            // Create database roles and assign schema permissions
            Execute.Sql("CREATE ROLE [SalesUser]");
            Execute.Sql("CREATE ROLE [SalesManager]");
            Execute.Sql("CREATE ROLE [SystemAdmin]");
            
            // Grant permissions
            Execute.Sql("GRANT SELECT, INSERT, UPDATE ON SCHEMA::[Sales_ReadWrite] TO [SalesManager]");
            Execute.Sql("GRANT SELECT ON SCHEMA::[Sales_ReadOnly] TO [SalesUser]");
            Execute.Sql("GRANT ALL ON SCHEMA::[Admin_Only] TO [SystemAdmin]");
        }
    }

    public override void Down()
    {
        if (IfDatabase("SqlServer"))
        {
            // Remove permissions and roles
            Execute.Sql("DROP ROLE IF EXISTS [SystemAdmin]");
            Execute.Sql("DROP ROLE IF EXISTS [SalesManager]");
            Execute.Sql("DROP ROLE IF EXISTS [SalesUser]");
            
            // Remove tables
            Delete.Table("AuditLog").InSchema("Audit");
            Delete.Table("UserRoles").InSchema("Security");
            Delete.Table("Users").InSchema("Security");
            
            // Remove schemas
            Execute.Sql("DROP SCHEMA IF EXISTS [Admin_Only]");
            Execute.Sql("DROP SCHEMA IF EXISTS [Sales_ReadWrite]");
            Execute.Sql("DROP SCHEMA IF EXISTS [Sales_ReadOnly]");
            
            Delete.Schema("Audit");
            Delete.Schema("Security");
            Delete.Schema("Application");
        }
    }
}
```

### PostgreSQL Schema Features

```csharp
public class PostgreSqlSchemaFeatures : Migration
{
    public override void Up()
    {
        if (IfDatabase("Postgres"))
        {
            // Create schemas with specific ownership and search path considerations
            Create.Schema("public_data");
            Create.Schema("private_data");
            Create.Schema("analytics");
            Create.Schema("staging");
            
            // Create tables with PostgreSQL-specific features
            Create.Table("user_profiles")
                .InSchema("private_data")
                .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("user_id").AsInt32().NotNullable()
                .WithColumn("profile_data").AsCustom("JSONB").Nullable()
                .WithColumn("preferences").AsCustom("JSONB").Nullable()
                .WithColumn("tags").AsCustom("TEXT[]").Nullable()
                .WithColumn("created_at").AsCustom("TIMESTAMP WITH TIME ZONE").NotNullable();
                
            Create.Table("analytics_events")
                .InSchema("analytics")
                .WithColumn("id").AsCustom("BIGSERIAL").NotNullable().PrimaryKey()
                .WithColumn("event_type").AsString(50).NotNullable()
                .WithColumn("event_data").AsCustom("JSONB").NotNullable()
                .WithColumn("user_id").AsInt32().Nullable()
                .WithColumn("session_id").AsCustom("UUID").Nullable()
                .WithColumn("ip_address").AsCustom("INET").Nullable()
                .WithColumn("occurred_at").AsCustom("TIMESTAMP WITH TIME ZONE").NotNullable();
                
            // Create indexes optimized for JSONB and array operations
            Execute.Sql(@"
                CREATE INDEX ix_user_profiles_profile_data 
                ON private_data.user_profiles USING GIN (profile_data)");
                
            Execute.Sql(@"
                CREATE INDEX ix_user_profiles_tags 
                ON private_data.user_profiles USING GIN (tags)");
                
            Execute.Sql(@"
                CREATE INDEX ix_analytics_events_event_data 
                ON analytics.analytics_events USING GIN (event_data)");
                
            // Set up Row Level Security (RLS)
            Execute.Sql("ALTER TABLE private_data.user_profiles ENABLE ROW LEVEL SECURITY");
            
            Execute.Sql(@"
                CREATE POLICY user_profiles_policy ON private_data.user_profiles
                FOR ALL TO public
                USING (user_id = current_setting('app.current_user_id')::INT)");
                
            // Create database roles with schema-specific permissions
            Execute.Sql("CREATE ROLE analytics_reader");
            Execute.Sql("CREATE ROLE data_manager");
            Execute.Sql("CREATE ROLE staging_user");
            
            Execute.Sql("GRANT USAGE ON SCHEMA analytics TO analytics_reader");
            Execute.Sql("GRANT SELECT ON ALL TABLES IN SCHEMA analytics TO analytics_reader");
            
            Execute.Sql("GRANT USAGE ON SCHEMA private_data TO data_manager");
            Execute.Sql("GRANT ALL ON ALL TABLES IN SCHEMA private_data TO data_manager");
            
            Execute.Sql("GRANT ALL ON SCHEMA staging TO staging_user");
        }
    }

    public override void Down()
    {
        if (IfDatabase("Postgres"))
        {
            // Clean up roles and permissions
            Execute.Sql("DROP ROLE IF EXISTS staging_user");
            Execute.Sql("DROP ROLE IF EXISTS data_manager");
            Execute.Sql("DROP ROLE IF EXISTS analytics_reader");
            
            // Remove tables and indexes
            Delete.Table("analytics_events").InSchema("analytics");
            Delete.Table("user_profiles").InSchema("private_data");
            
            // Remove schemas
            Delete.Schema("staging");
            Delete.Schema("analytics");
            Delete.Schema("private_data");
            Delete.Schema("public_data");
        }
    }
}
```

### Oracle Schema Equivalents

```csharp
public class OracleSchemaFeatures : Migration
{
    public override void Up()
    {
        if (IfDatabase("Oracle"))
        {
            // Oracle uses users as schema owners, so we create logical separation with table prefixes
            // or use different tablespaces
            
            // Create tablespaces for logical schema separation
            Execute.Sql("CREATE TABLESPACE SALES_DATA DATAFILE 'sales_data.dbf' SIZE 100M AUTOEXTEND ON");
            Execute.Sql("CREATE TABLESPACE HR_DATA DATAFILE 'hr_data.dbf' SIZE 50M AUTOEXTEND ON");
            Execute.Sql("CREATE TABLESPACE INVENTORY_DATA DATAFILE 'inventory_data.dbf' SIZE 75M AUTOEXTEND ON");
            
            // Create tables with schema-like prefixes and specific tablespaces
            Create.Table("SALES_CUSTOMERS")
                .WithColumn("ID").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("COMPANY_NAME").AsString(200).NotNullable()
                .WithColumn("CONTACT_NAME").AsString(100).NotNullable()
                .WithColumn("EMAIL").AsString(255).NotNullable()
                .WithColumn("CREATED_DATE").AsDateTime().NotNullable();
                
            Execute.Sql("ALTER TABLE SALES_CUSTOMERS MOVE TABLESPACE SALES_DATA");
            
            Create.Table("HR_EMPLOYEES")
                .WithColumn("ID").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("FIRST_NAME").AsString(50).NotNullable()
                .WithColumn("LAST_NAME").AsString(50).NotNullable()
                .WithColumn("DEPARTMENT").AsString(50).NotNullable()
                .WithColumn("HIRE_DATE").AsDateTime().NotNullable()
                .WithColumn("SALARY").AsDecimal(10, 2).Nullable();
                
            Execute.Sql("ALTER TABLE HR_EMPLOYEES MOVE TABLESPACE HR_DATA");
            
            Create.Table("INVENTORY_PRODUCTS")
                .WithColumn("ID").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("NAME").AsString(100).NotNullable()
                .WithColumn("SKU").AsString(50).NotNullable()
                .WithColumn("PRICE").AsDecimal(10, 2).NotNullable()
                .WithColumn("QUANTITY").AsInt32().NotNullable();
                
            Execute.Sql("ALTER TABLE INVENTORY_PRODUCTS MOVE TABLESPACE INVENTORY_DATA");
            
            // Create synonyms for easier access
            Execute.Sql("CREATE PUBLIC SYNONYM CUSTOMERS FOR SALES_CUSTOMERS");
            Execute.Sql("CREATE PUBLIC SYNONYM EMPLOYEES FOR HR_EMPLOYEES");
            Execute.Sql("CREATE PUBLIC SYNONYM PRODUCTS FOR INVENTORY_PRODUCTS");
        }
    }

    public override void Down()
    {
        if (IfDatabase("Oracle"))
        {
            Execute.Sql("DROP PUBLIC SYNONYM PRODUCTS");
            Execute.Sql("DROP PUBLIC SYNONYM EMPLOYEES");
            Execute.Sql("DROP PUBLIC SYNONYM CUSTOMERS");
            
            Delete.Table("INVENTORY_PRODUCTS");
            Delete.Table("HR_EMPLOYEES");
            Delete.Table("SALES_CUSTOMERS");
            
            Execute.Sql("DROP TABLESPACE INVENTORY_DATA INCLUDING CONTENTS AND DATAFILES");
            Execute.Sql("DROP TABLESPACE HR_DATA INCLUDING CONTENTS AND DATAFILES");
            Execute.Sql("DROP TABLESPACE SALES_DATA INCLUDING CONTENTS AND DATAFILES");
        }
    }
}
```

## Schema Migration Strategies

### Gradual Schema Refactoring

```csharp
public class GradualSchemaRefactoring : Migration
{
    public override void Up()
    {
        // Phase 1: Create new schemas
        Create.Schema("NewSales");
        Create.Schema("NewInventory");
        
        // Phase 2: Create new tables in new schemas
        Create.Table("Customers")
            .InSchema("NewSales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        Create.Table("Products")
            .InSchema("NewInventory")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("SKU").AsString(50).NotNullable()
            .WithColumn("Price").AsDecimal(10, 2).NotNullable();
            
        // Phase 3: Migrate data from old tables (if they exist)
        if (Schema.Table("OldCustomers").Exists())
        {
            Execute.Sql(@"
                INSERT INTO NewSales.Customers (Name, Email, CreatedAt)
                SELECT Name, Email, CreatedAt FROM OldCustomers");
        }
        
        if (Schema.Table("OldProducts").Exists())
        {
            Execute.Sql(@"
                INSERT INTO NewInventory.Products (Name, SKU, Price)
                SELECT Name, SKU, Price FROM OldProducts");
        }
        
        // Phase 4: Create views for backward compatibility
        Execute.Sql(@"
            CREATE VIEW Customers AS 
            SELECT Id, Name, Email, CreatedAt 
            FROM NewSales.Customers");
            
        Execute.Sql(@"
            CREATE VIEW Products AS 
            SELECT Id, Name, SKU, Price 
            FROM NewInventory.Products");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS Products");
        Execute.Sql("DROP VIEW IF EXISTS Customers");
        
        Delete.Table("Products").InSchema("NewInventory");
        Delete.Table("Customers").InSchema("NewSales");
        
        Delete.Schema("NewInventory");
        Delete.Schema("NewSales");
    }
}
```

### Schema Consolidation

```csharp
public class SchemaConsolidation : Migration
{
    public override void Up()
    {
        // Consolidate multiple schemas into a unified schema
        Create.Schema("Unified");
        
        // Move tables from various schemas to unified schema
        var schemasToConsolidate = new[] { "Sales", "Marketing", "Customer" };
        
        foreach (var schemaName in schemasToConsolidate)
        {
            if (Schema.Schema(schemaName).Exists())
            {
                // Example: Move Customers table
                if (Schema.Table("Customers").InSchema(schemaName).Exists())
                {
                    // Create new table in unified schema
                    Create.Table($"{schemaName}_Customers")
                        .InSchema("Unified")
                        .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                        .WithColumn("Name").AsString(100).NotNullable()
                        .WithColumn("Email").AsString(255).NotNullable()
                        .WithColumn("SourceSchema").AsString(50).NotNullable().WithDefaultValue(schemaName)
                        .WithColumn("CreatedAt").AsDateTime().NotNullable();
                        
                    // Migrate data
                    Execute.Sql($@"
                        INSERT INTO Unified.{schemaName}_Customers (Id, Name, Email, CreatedAt)
                        SELECT Id, Name, Email, CreatedAt 
                        FROM {schemaName}.Customers");
                }
            }
        }
        
        // Create a unified view that combines all customer sources
        Execute.Sql(@"
            CREATE VIEW Unified.AllCustomers AS
            SELECT 'Sales' as Source, Id, Name, Email, CreatedAt FROM Unified.Sales_Customers
            UNION ALL
            SELECT 'Marketing' as Source, Id, Name, Email, CreatedAt FROM Unified.Marketing_Customers
            UNION ALL
            SELECT 'Customer' as Source, Id, Name, Email, CreatedAt FROM Unified.Customer_Customers");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS Unified.AllCustomers");
        
        var schemasToConsolidate = new[] { "Sales", "Marketing", "Customer" };
        
        foreach (var schemaName in schemasToConsolidate)
        {
            if (Schema.Table($"{schemaName}_Customers").InSchema("Unified").Exists())
            {
                Delete.Table($"{schemaName}_Customers").InSchema("Unified");
            }
        }
        
        Delete.Schema("Unified");
    }
}
```

## Best Practices for Schema Management

### Schema Organization Guidelines

```csharp
public class SchemaOrganizationBestPractices : Migration
{
    public override void Up()
    {
        // 1. Business Domain Separation
        Create.Schema("CustomerDomain");    // Customer-related entities
        Create.Schema("OrderDomain");       // Order processing entities
        Create.Schema("ProductDomain");     // Product catalog entities
        Create.Schema("PaymentDomain");     // Payment processing entities
        
        // 2. Technical Separation
        Create.Schema("Configuration");     // Application configuration
        Create.Schema("Audit");            // Audit and logging tables
        Create.Schema("Temporary");        // Temporary processing tables
        Create.Schema("Archive");          // Historical data storage
        
        // 3. Security Separation
        Create.Schema("PublicData");       // Publicly accessible data
        Create.Schema("InternalData");     // Internal use only
        Create.Schema("SensitiveData");    // Sensitive information
        
        // Example implementation of organized tables
        Create.Table("Customers")
            .InSchema("CustomerDomain")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CompanyName").AsString(200).NotNullable()
            .WithColumn("ContactName").AsString(100).NotNullable();
            
        Create.Table("CustomerPII")
            .InSchema("SensitiveData")
            .WithColumn("CustomerId").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("SSN").AsString(11).Nullable()
            .WithColumn("CreditCardLast4").AsString(4).Nullable()
            .WithColumn("EncryptedData").AsCustom("VARBINARY(MAX)").Nullable();
            
        Create.Table("ApplicationSettings")
            .InSchema("Configuration")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("SettingName").AsString(100).NotNullable()
            .WithColumn("SettingValue").AsString(1000).Nullable()
            .WithColumn("Environment").AsString(20).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ApplicationSettings").InSchema("Configuration");
        Delete.Table("CustomerPII").InSchema("SensitiveData");
        Delete.Table("Customers").InSchema("CustomerDomain");
        
        var schemas = new[] { 
            "SensitiveData", "InternalData", "PublicData",
            "Archive", "Temporary", "Audit", "Configuration",
            "PaymentDomain", "ProductDomain", "OrderDomain", "CustomerDomain"
        };
        
        foreach (var schema in schemas)
        {
            Delete.Schema(schema);
        }
    }
}
```

### Schema Naming Conventions

```csharp
public class SchemaNamingConventions : Migration
{
    public override void Up()
    {
        // Consistent naming patterns
        
        // 1. Domain-based naming (recommended)
        Create.Schema("Sales");
        Create.Schema("Inventory");
        Create.Schema("HR");
        Create.Schema("Finance");
        
        // 2. Application module-based naming
        Create.Schema("UserManagement");
        Create.Schema("ContentManagement");
        Create.Schema("ReportingEngine");
        Create.Schema("NotificationService");
        
        // 3. Data lifecycle-based naming
        Create.Schema("Operational");      // Current operational data
        Create.Schema("Analytical");       // Data for analysis/reporting
        Create.Schema("Staging");          // ETL staging area
        Create.Schema("Archive");          // Historical/archived data
        
        // 4. Access pattern-based naming
        Create.Schema("ReadOnly");         // Reference data, lookups
        Create.Schema("ReadWrite");        // Transactional data
        Create.Schema("BulkOperations");   // Batch processing data
        
        // Create sample tables demonstrating the naming patterns
        Create.Table("SalesOrders")
            .InSchema("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("OrderNumber").AsString(20).NotNullable();
            
        Create.Table("UserAccounts")
            .InSchema("UserManagement")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Username").AsString(50).NotNullable();
            
        Create.Table("DailySummary")
            .InSchema("Analytical")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("SummaryDate").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("DailySummary").InSchema("Analytical");
        Delete.Table("UserAccounts").InSchema("UserManagement");
        Delete.Table("SalesOrders").InSchema("Sales");
        
        var schemas = new[] {
            "BulkOperations", "ReadWrite", "ReadOnly",
            "Archive", "Staging", "Analytical", "Operational",
            "NotificationService", "ReportingEngine", "ContentManagement", "UserManagement",
            "Finance", "HR", "Inventory", "Sales"
        };
        
        foreach (var schema in schemas)
        {
            Delete.Schema(schema);
        }
    }
}
```

## Schema Validation and Maintenance

### Schema Consistency Checks

```csharp
public class SchemaValidation : Migration
{
    public override void Up()
    {
        // Validate schema organization before proceeding
        var requiredSchemas = new[] { "Sales", "Inventory", "HR" };
        
        foreach (var schemaName in requiredSchemas)
        {
            if (!Schema.Schema(schemaName).Exists())
            {
                Create.Schema(schemaName);
            }
        }
        
        // Validate cross-schema references
        if (Schema.Table("Orders").InSchema("Sales").Exists() && 
            !Schema.Table("Products").InSchema("Inventory").Exists())
        {
            throw new InvalidOperationException("Orders table exists but referenced Products table is missing in Inventory schema");
        }
        
        // Create consistent table structure across schemas
        foreach (var schemaName in requiredSchemas)
        {
            if (!Schema.Table("AuditLog").InSchema(schemaName).Exists())
            {
                Create.Table("AuditLog")
                    .InSchema(schemaName)
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("TableName").AsString(100).NotNullable()
                    .WithColumn("Action").AsString(20).NotNullable()
                    .WithColumn("RecordId").AsInt32().NotNullable()
                    .WithColumn("ChangedBy").AsString(100).NotNullable()
                    .WithColumn("ChangedAt").AsDateTime().NotNullable();
            }
        }
    }

    public override void Down()
    {
        var schemas = new[] { "Sales", "Inventory", "HR" };
        
        foreach (var schemaName in schemas)
        {
            if (Schema.Table("AuditLog").InSchema(schemaName).Exists())
            {
                Delete.Table("AuditLog").InSchema(schemaName);
            }
        }
    }
}
```

## Advanced Schema Operations with Execute.Sql

For comprehensive examples of advanced schema operations using Execute.Sql including:
- Database-specific schema management
- Complex role and permission management
- Advanced indexing strategies
- Full-text search setup
- Custom constraint creation

See: [Raw SQL (Scripts & Helpers)](../raw-sql-scripts.md)

## See Also

- [Creating Tables](create-tables.md)
- [Altering Tables](alter-tables.md)
- [Managing Columns](columns.md)
- [Working with Indexes](indexes.md)
- [Foreign Keys](foreign-keys.md)
- [Data Operations](data.md)
- [Best Practices](../advanced/best-practices.md)
- [Database Providers](../providers/)