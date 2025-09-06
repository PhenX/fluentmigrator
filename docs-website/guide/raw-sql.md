# Raw SQL Helper

When FluentMigrator's fluent API doesn't cover a specific scenario, or when you need to use database-specific functions, the `RawSql` helper class provides a way to embed raw SQL expressions within your fluent migrations.

## Purpose and Usage

The `RawSql` helper is primarily used when:
- Inserting data that requires database functions (like `CURRENT_USER`, `GETDATE()`)
- Setting default values with SQL expressions
- Working with database-specific features not covered by the fluent API

## Basic RawSql.Insert Usage

### Problem: String Literals vs SQL Expressions

When you use regular strings in `Insert.IntoTable()`, FluentMigrator treats them as literal string values:

```csharp
// ❌ This inserts the literal string "CURRENT_USER"
Insert.IntoTable("Users").Row(new { Username = "CURRENT_USER" });
// Result: Username column contains the text "CURRENT_USER"
```

### Solution: Using RawSql.Insert

```csharp
// ✅ This executes the CURRENT_USER function
Insert.IntoTable("Users").Row(new { Username = RawSql.Insert("CURRENT_USER") });
// Result: Username column contains the actual current database user
```

## Common Database Functions

### SQL Server Functions
```csharp
Insert.IntoTable("AuditLog").Row(new
{
    Action = "Login",
    Username = RawSql.Insert("SUSER_SNAME()"),     // Current SQL Server user
    Timestamp = RawSql.Insert("GETUTCDATE()"),     // UTC timestamp
    MachineName = RawSql.Insert("HOST_NAME()"),    // Machine name
    DatabaseName = RawSql.Insert("DB_NAME()"),     // Current database
    SessionId = RawSql.Insert("@@SPID")            // Session ID
});
```

### PostgreSQL Functions
```csharp
Insert.IntoTable("Sessions").Row(new
{
    UserId = 1,
    CreatedAt = RawSql.Insert("NOW()"),            // Current timestamp
    UserName = RawSql.Insert("current_user"),      // Current user
    DatabaseName = RawSql.Insert("current_database()"), // Database name
    SessionId = RawSql.Insert("pg_backend_pid()"), // Process ID
    ClientAddress = RawSql.Insert("inet_client_addr()") // Client IP
});
```

### MySQL Functions
```csharp
Insert.IntoTable("LoginHistory").Row(new
{
    LoginTime = RawSql.Insert("NOW()"),            // Current timestamp
    User = RawSql.Insert("USER()"),               // Current user
    Connection = RawSql.Insert("CONNECTION_ID()"), // Connection ID
    Database = RawSql.Insert("DATABASE()"),       // Current database
    Version = RawSql.Insert("VERSION()")          // MySQL version
});
```

### SQLite Functions
```csharp
Insert.IntoTable("Events").Row(new
{
    EventTime = RawSql.Insert("datetime('now')"),  // Current datetime
    EventDate = RawSql.Insert("date('now')"),      // Current date
    Timestamp = RawSql.Insert("strftime('%s','now')"), // Unix timestamp
    RandomId = RawSql.Insert("hex(randomblob(16))") // Random hex string
});
```

## Default Values with RawSql

While `WithDefaultValue()` works for simple values, `RawSql` can be used for complex default expressions:

### Simple Default Values (Preferred)
```csharp
Create.Table("Orders")
    .WithIdColumn()
    .WithColumn("OrderDate")
        .AsDateTime()
        .NotNullable()
        .WithDefaultValue(SystemMethods.CurrentDateTime) // FluentMigrator built-in
    .WithColumn("Status")
        .AsString(20)
        .NotNullable()
        .WithDefaultValue("Pending"); // Literal string
```

### Complex Default Values with RawSql
For more complex scenarios, you might need to combine `Execute.Sql()` with table creation:

```csharp
// First create the table
Create.Table("Products")
    .WithIdColumn()
    .WithColumn("CreatedAt").AsDateTime().NotNullable()
    .WithColumn("ProductCode").AsString(50).NotNullable()
    .WithColumn("Version").AsString(20).NotNullable();

// Then add complex defaults via ALTER TABLE
Execute.Sql(@"
    ALTER TABLE Products 
    ADD CONSTRAINT DF_Products_CreatedAt 
    DEFAULT GETUTCDATE() FOR CreatedAt
");

Execute.Sql(@"
    ALTER TABLE Products 
    ADD CONSTRAINT DF_Products_ProductCode 
    DEFAULT ('PROD-' + CAST(NEWID() AS VARCHAR(36))) FOR ProductCode
");
```

## Cross-Database Compatibility

When using `RawSql`, be aware that your migrations become database-specific. Use conditional logic for cross-database support:

```csharp
public override void Up()
{
    Create.Table("Sessions")
        .WithIdColumn()
        .WithColumn("CreatedAt").AsDateTime().NotNullable()
        .WithColumn("UserName").AsString(100).Nullable();

    // Insert initial session with database-specific functions
    IfDatabase("SqlServer")
        .Insert.IntoTable("Sessions").Row(new
        {
            CreatedAt = RawSql.Insert("GETUTCDATE()"),
            UserName = RawSql.Insert("SUSER_SNAME()")
        });

    IfDatabase("Postgres")
        .Insert.IntoTable("Sessions").Row(new
        {
            CreatedAt = RawSql.Insert("NOW()"),
            UserName = RawSql.Insert("current_user")
        });

    IfDatabase("MySql")
        .Insert.IntoTable("Sessions").Row(new
        {
            CreatedAt = RawSql.Insert("UTC_TIMESTAMP()"),
            UserName = RawSql.Insert("USER()")
        });

    IfDatabase("Sqlite")
        .Insert.IntoTable("Sessions").Row(new
        {
            CreatedAt = RawSql.Insert("datetime('now')"),
            UserName = RawSql.Insert("'sqlite_user'") // SQLite doesn't have user functions
        });
}
```

## Advanced Examples

### UUID Generation
```csharp
Insert.IntoTable("Customers").Row(new
{
    CustomerId = RawSql.Insert("NEWID()"), // SQL Server
    Name = "Test Customer",
    CreatedAt = RawSql.Insert("GETUTCDATE()")
});

// PostgreSQL equivalent
Insert.IntoTable("Customers").Row(new
{
    CustomerId = RawSql.Insert("gen_random_uuid()"), // PostgreSQL
    Name = "Test Customer", 
    CreatedAt = RawSql.Insert("NOW()")
});
```

### Calculated Values
```csharp
Insert.IntoTable("OrderSummary").Row(new
{
    OrderId = 1001,
    TotalAmount = RawSql.Insert("(SELECT SUM(Price * Quantity) FROM OrderItems WHERE OrderId = 1001)"),
    ItemCount = RawSql.Insert("(SELECT COUNT(*) FROM OrderItems WHERE OrderId = 1001)"),
    UpdatedAt = RawSql.Insert("GETUTCDATE()")
});
```

### Conditional Inserts
```csharp
public override void Up()
{
    // Only insert if record doesn't exist
    Execute.Sql(@"
        IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingKey = 'AppVersion')
        BEGIN
            INSERT INTO Settings (SettingKey, SettingValue, CreatedAt)
            VALUES ('AppVersion', '1.0.0', GETUTCDATE())
        END
    ");
}
```

## Limitations of RawSql Helper

::: warning Important Limitations
The `RawSql.Insert` method has some limitations:

1. **Cannot be used in Update/Delete operations**: `RawSql.Insert` cannot be used in the `Set` or `Where` clauses of `Update` or `Delete` expressions
2. **Insert operations only**: Currently only supports insert scenarios
3. **No type checking**: The SQL expression is passed through as-is without validation
:::

### What Doesn't Work
```csharp
// ❌ This will NOT work
Update.Table("Users")
    .Set(new { LastLogin = RawSql.Insert("GETUTCDATE()") }) // Won't work!
    .Where(new { Id = 1 });

// ❌ This will NOT work  
Delete.FromTable("Users")
    .Row(new { CreatedAt = RawSql.Insert("< DATEADD(day, -30, GETUTCDATE())") }); // Won't work!
```

### Alternatives for Update/Delete
```csharp
// ✅ Use Execute.Sql for updates with functions
Execute.Sql("UPDATE Users SET LastLogin = GETUTCDATE() WHERE Id = 1");

// ✅ Use Execute.Sql for complex delete conditions  
Execute.Sql("DELETE FROM Users WHERE CreatedAt < DATEADD(day, -30, GETUTCDATE())");
```

## Best Practices

### ✅ Do
- Use `RawSql.Insert` for database functions in insert operations
- Combine with `IfDatabase()` for cross-database compatibility
- Document database-specific dependencies
- Test with your target database(s)
- Use `Execute.Sql()` for complex operations not supported by `RawSql.Insert`

### ❌ Don't  
- Use `RawSql.Insert` in Update or Delete operations (it won't work)
- Hardcode database-specific syntax without conditional logic
- Forget that RawSql makes your migrations database-specific
- Use RawSql when FluentMigrator's built-in methods would work

### Database-Agnostic Alternatives

When possible, use FluentMigrator's built-in methods for better portability:

```csharp
// ✅ Better: Database-agnostic
.WithColumn("CreatedAt")
    .AsDateTime()
    .NotNullable()
    .WithDefaultValue(SystemMethods.CurrentDateTime)

// vs database-specific RawSql in Execute.Sql
```

## Integration with Profiles and Environments

```csharp
[Profile("Development")]
public class SeedDevelopmentData : Migration
{
    public override void Up()
    {
        Insert.IntoTable("Users").Row(new
        {
            Username = "devuser",
            Email = "dev@example.com",
            CreatedAt = RawSql.Insert("GETUTCDATE()"),
            PasswordHash = RawSql.Insert("HASHBYTES('SHA2_256', 'devpassword')") // SQL Server
        });
    }

    public override void Down() { }
}
```

The `RawSql` helper is a powerful tool when you need to bridge the gap between FluentMigrator's fluent API and raw SQL functionality, but use it judiciously to maintain database portability where possible.