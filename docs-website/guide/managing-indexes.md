# Managing Indexes

Indexes are crucial for database performance optimization. FluentMigrator provides comprehensive support for creating, modifying, and managing database indexes across different database providers.

## Basic Index Operations

### Creating Simple Indexes

```csharp
public class BasicIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FirstName").AsString(50).NotNullable()
            .WithColumn("LastName").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable();
            
        // Single column index
        Create.Index("IX_Users_LastName")
            .OnTable("Users")
            .OnColumn("LastName");
            
        // Multi-column index
        Create.Index("IX_Users_FirstName_LastName")
            .OnTable("Users")
            .OnColumn("FirstName")
            .OnColumn("LastName");
            
        // Index with sort order
        Create.Index("IX_Users_CreatedAt_Desc")
            .OnTable("Users")
            .OnColumn("CreatedAt").Descending();
    }

    public override void Down()
    {
        Delete.Index("IX_Users_LastName").OnTable("Users");
        Delete.Index("IX_Users_FirstName_LastName").OnTable("Users");
        Delete.Index("IX_Users_CreatedAt_Desc").OnTable("Users");
        Delete.Table("Users");
    }
}
```

### Unique Indexes

```csharp
public class UniqueIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Products")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("SKU").AsString(50).NotNullable()
            .WithColumn("CompanyId").AsInt32().NotNullable()
            .WithColumn("InternalCode").AsString(20).NotNullable();
            
        // Simple unique index
        Create.Index("UQ_Products_SKU")
            .OnTable("Products")
            .OnColumn("SKU")
            .Unique();
            
        // Composite unique index
        Create.Index("UQ_Products_CompanyId_InternalCode")
            .OnTable("Products")
            .OnColumn("CompanyId")
            .OnColumn("InternalCode")
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("UQ_Products_SKU").OnTable("Products");
        Delete.Index("UQ_Products_CompanyId_InternalCode").OnTable("Products");
        Delete.Table("Products");
    }
}
```

### Filtered Indexes

```csharp
public class FilteredIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable()
            .WithColumn("CompletedDate").AsDateTime().Nullable();
            
        // Filtered index for active orders only
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Create.Index("IX_Orders_CustomerId_Active")
                .OnTable("Orders")
                .OnColumn("CustomerId")
                .WithOptions()
                .Filter("Status = 'Active'");
                
            // Filtered index excluding NULL values
            Create.Index("IX_Orders_CompletedDate")
                .OnTable("Orders")
                .OnColumn("CompletedDate")
                .WithOptions()
                .Filter("CompletedDate IS NOT NULL");
    });
        
        // PostgreSQL partial indexes
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                CREATE INDEX IX_Orders_CustomerId_Active 
                ON Orders (CustomerId) 
                WHERE Status = 'Active'");
    }

    public override void Down()
    {
        IfDatabase(ProcessorIdConstants.SqlServer, ProcessorIdConstants.Postgres).Delegate(() =>
        {
            Delete.Index("IX_Orders_CustomerId_Active").OnTable("Orders");
            Delete.Index("IX_Orders_CompletedDate").OnTable("Orders");
        });
        Delete.Table("Orders");
    }
}
```

## Advanced Index Types

### Covering Indexes (SQL Server)

```csharp
public class CoveringIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Sales")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("UnitPrice").AsDecimal(10, 2).NotNullable()
            .WithColumn("TotalAmount").AsDecimal(10, 2).NotNullable()
            .WithColumn("SaleDate").AsDateTime().NotNullable()
            .WithColumn("SalesPersonId").AsInt32().NotNullable();
            
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
// Covering index with included columns
            Create.Index("IX_Sales_CustomerId_SaleDate_Covering")
                .OnTable("Sales")
                .OnColumn("CustomerId")
                .OnColumn("SaleDate")
                .WithOptions()
                .Include("ProductId")
                .Include("Quantity")
                .Include("UnitPrice")
                .Include("TotalAmount");
    });
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Delete.Index("IX_Sales_CustomerId_SaleDate_Covering").OnTable("Sales");
    });
        Delete.Table("Sales");
    }
}
```

### Functional/Expression Indexes

```csharp
public class FunctionalIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Customers")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FirstName").AsString(50).NotNullable()
            .WithColumn("LastName").AsString(50).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Phone").AsString(20).Nullable();
            
        // Case-insensitive index for email searches
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                CREATE INDEX IX_Customers_Email_Lower 
                ON Customers (LOWER(Email))");
        
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                CREATE INDEX IX_Customers_Email_Lower 
                ON Customers (LOWER(Email))");
        
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
                CREATE INDEX IX_Customers_Email_Lower 
                ON Customers ((LOWER(Email)))");
    }

    public override void Down()
    {
        IfDatabase("SqlServer", "Postgres", "MySQL").Delegate(() =>
        {
            Delete.Index("IX_Customers_Email_Lower").OnTable("Customers");
        });
        
            IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Delete.Index("IX_Customers_FullName_Text").OnTable("Customers");
    });
        
        Delete.Table("Customers");
    }
}
```

### Full-Text Indexes

```csharp
public class FullTextIndexes : Migration
{
    public override void Up()
    {
        Create.Table("Articles")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Content").AsString(int.MaxValue).NotNullable()
            .WithColumn("Summary").AsString(1000).Nullable()
            .WithColumn("Tags").AsString(500).Nullable();
            
        // SQL Server Full-Text Index
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("CREATE FULLTEXT CATALOG ArticlesCatalog AS DEFAULT");
        
        // PostgreSQL Full-Text Search
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                CREATE INDEX IX_Articles_FullText 
                ON Articles USING GIN (
                    to_tsvector('english', 
                        COALESCE(Title, '') || ' ' || 
                        COALESCE(Content, '') || ' ' || 
                        COALESCE(Summary, '')
                    )
                )");
        
        // MySQL Full-Text Index
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
                CREATE FULLTEXT INDEX IX_Articles_FullText 
                ON Articles (Title, Content, Summary)");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("DROP FULLTEXT INDEX ON Articles");
        
            IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Delete.Index("IX_Articles_FullText").OnTable("Articles");
    });
        
            IfDatabase(ProcessorIdConstants.MySql).Delegate(() =>
    {
Delete.Index("IX_Articles_FullText").OnTable("Articles");
    });
        
        Delete.Table("Articles");
    }
}
```

## Database-Specific Index Features

### SQL Server Specific Indexes

```csharp
public class SqlServerIndexes : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                CREATE NONCLUSTERED COLUMNSTORE INDEX IX_LargeTable_Columnstore
                ON LargeTable (Name, Value, CreatedDate)");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Delete.Index("IX_LargeTable_CreatedDate_Clustered").OnTable("LargeTable");
            Delete.Index("IX_LargeTable_Columnstore").OnTable("LargeTable");
            Delete.Index("IX_LargeTable_Name_Compressed").OnTable("LargeTable");
            Delete.Table("LargeTable");
    });
    }
}
```

### PostgreSQL Specific Indexes

```csharp
public class PostgreSqlIndexes : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql(@"
                CREATE INDEX IX_PostgresTable_Tags 
                ON PostgresTable USING GIN (Tags)");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.Postgres).Delegate(() =>
    {
Delete.Index("IX_PostgresTable_Tags").OnTable("PostgresTable");
            Delete.Index("IX_PostgresTable_Data").OnTable("PostgresTable");
            Delete.Index("IX_PostgresTable_Location").OnTable("PostgresTable");
            Delete.Index("IX_PostgresTable_Name_Hash").OnTable("PostgresTable");
            Delete.Index("IX_PostgresTable_Name_Unique_Active").OnTable("PostgresTable");
            Delete.Table("PostgresTable");
    });
    }
}
```

### MySQL Specific Indexes

```csharp
public class MySqlIndexes : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql(@"
                CREATE FULLTEXT INDEX IX_MySqlTable_FullText 
                ON MySqlTable (Name, Description)");
    }

    public override void Down()
    {
            IfDatabase(ProcessorIdConstants.MySql).Delegate(() =>
    {
Delete.Index("IX_MySqlTable_FullText").OnTable("MySqlTable");
            Delete.Index("IX_MySqlTable_Spatial").OnTable("MySqlTable");
            Delete.Index("IX_MySqlTable_Description_Prefix").OnTable("MySqlTable");
            Delete.Table("MySqlTable");
    });
    }
}
```

## Index Management and Maintenance

### Rebuilding Indexes

```csharp
public class IndexMaintenance : Migration
{
    public override void Up()
    {
        // Create initial index
        Create.Index("IX_Users_Name")
            .OnTable("Users")
            .OnColumn("FirstName")
            .OnColumn("LastName");
    }

    public override void Down()
    {
        Delete.Index("IX_Users_Name").OnTable("Users");
    }
}

// Separate migration for rebuilding indexes
public class RebuildIndexes : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("ALTER INDEX ALL ON Users REBUILD");
        
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql("REINDEX TABLE Users");
    }

    public override void Down()
    {
        // Rebuilding indexes is typically not rolled back
    }
}
```

### Index Statistics and Analysis

```csharp
public class IndexStatistics : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql("UPDATE STATISTICS Users");
        
            IfDatabase(ProcessorIdConstants.Postgres).Execute.Sql("ANALYZE Users");
        
            IfDatabase(ProcessorIdConstants.MySql).Execute.Sql("ANALYZE TABLE Users");
    }

    public override void Down()
    {
        // Statistics updates are typically not rolled back
    }
}
```

## Performance Optimization Strategies

### Index Selection Guidelines

```csharp
public class IndexOptimization : Migration
{
    public override void Up()
    {
        Create.Table("OptimizedTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("CategoryId").AsInt32().NotNullable()
            .WithColumn("Status").AsString(20).NotNullable()
            .WithColumn("CreatedDate").AsDateTime().NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable();
            
        // Foreign key indexes (most selective columns first)
        Create.Index("IX_OptimizedTable_UserId")
            .OnTable("OptimizedTable")
            .OnColumn("UserId");
            
        // Composite index for common query patterns
        // Order by selectivity: most selective columns first
        Create.Index("IX_OptimizedTable_Status_CategoryId_CreatedDate")
            .OnTable("OptimizedTable")
            .OnColumn("Status")      // Most selective
            .OnColumn("CategoryId")  // Moderately selective
            .OnColumn("CreatedDate"); // Least selective but used in ORDER BY
            
        // Covering index for summary queries
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Create.Index("IX_OptimizedTable_CategoryId_Covering")
                .OnTable("OptimizedTable")
                .OnColumn("CategoryId")
                .OnColumn("Status")
                .WithOptions()
                .Include("Amount")
                .Include("CreatedDate");
    });
    }

    public override void Down()
    {
        Delete.Table("OptimizedTable");
    }
}
```

### Conditional Index Creation

```csharp
public class ConditionalIndexes : Migration
{
    public override void Up()
    {
        Create.Table("ConditionalTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable()
            .WithColumn("LargeDataColumn").AsString(4000).Nullable();
            
        // Only create index if table will have significant data
        var expectedRows = 100000; // This could come from configuration
        
        if (expectedRows > 10000)
        {
            Create.Index("IX_ConditionalTable_Name")
                .OnTable("ConditionalTable")
                .OnColumn("Name");
                
            // Create partial index for active records only
                IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Create.Index("IX_ConditionalTable_Active")
                    .OnTable("ConditionalTable")
                    .OnColumn("IsActive")
                    .WithOptions()
                    .Filter("IsActive = 1");
    });
        }
    }

    public override void Down()
    {
        if (Schema.Table("ConditionalTable").Index("IX_ConditionalTable_Name").Exists())
        {
            Delete.Index("IX_ConditionalTable_Name").OnTable("ConditionalTable");
        }
        
        if (Schema.Table("ConditionalTable").Index("IX_ConditionalTable_Active").Exists())
        {
            Delete.Index("IX_ConditionalTable_Active").OnTable("ConditionalTable");
        }
        
        Delete.Table("ConditionalTable");
    }
}
```

## Index Naming Conventions

### Consistent Naming Strategy

```csharp
public class IndexNamingConventions : Migration
{
    public override void Up()
    {
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsInt32().NotNullable()
            .WithColumn("ProductId").AsInt32().NotNullable()
            .WithColumn("StatusId").AsInt32().NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable();
            
        // Naming convention: IX_TableName_ColumnName(s)_Purpose
        Create.Index("IX_Orders_CustomerId")
            .OnTable("Orders")
            .OnColumn("CustomerId");
            
        Create.Index("IX_Orders_CustomerId_OrderDate_Performance")
            .OnTable("Orders")
            .OnColumn("CustomerId")
            .OnColumn("OrderDate");
            
        // Unique index naming: UQ_TableName_ColumnName(s)
        Create.Index("UQ_Orders_CustomerId_ProductId")
            .OnTable("Orders")
            .OnColumn("CustomerId")
            .OnColumn("ProductId")
            .Unique();
            
        // Filtered index naming: IX_TableName_ColumnName_FilterCondition
            IfDatabase(ProcessorIdConstants.SqlServer).Delegate(() =>
    {
Create.Index("IX_Orders_OrderDate_Active")
                .OnTable("Orders")
                .OnColumn("OrderDate")
                .WithOptions()
                .Filter("StatusId = 1");
    });
    }

    public override void Down()
    {
        Delete.Table("Orders");
    }
}
```

## Best Practices for Index Management

### 1. Monitor Index Usage

```csharp
public class IndexMonitoring : Migration
{
    public override void Up()
    {
        // Create indexes with monitoring in mind
        Create.Index("IX_Users_LastLogin")
            .OnTable("Users")
            .OnColumn("LastLoginDate");
            
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                -- Use this query to monitor index usage:
                -- SELECT 
                --     i.name AS IndexName,
                --     ius.user_seeks,
                --     ius.user_scans,
                --     ius.user_lookups,
                --     ius.user_updates
                -- FROM sys.indexes i
                -- LEFT JOIN sys.dm_db_index_usage_stats ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id
                -- WHERE OBJECT_NAME(i.object_id) = 'Users'
                ");
    }

    public override void Down()
    {
        Delete.Index("IX_Users_LastLogin").OnTable("Users");
    }
}
```

### 2. Index Maintenance Strategy

```csharp
public class IndexMaintenanceStrategy : Migration
{
    public override void Up()
    {
        // Create indexes with maintenance considerations
        Create.Table("HighVolumeTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("TransactionDate").AsDateTime().NotNullable()
            .WithColumn("Amount").AsDecimal(10, 2).NotNullable()
            .WithColumn("Status").AsString(20).NotNullable();
            
        // Create indexes with appropriate fill factor for high-volume tables
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                CREATE INDEX IX_HighVolumeTable_TransactionDate 
                ON HighVolumeTable (TransactionDate) 
                WITH (FILLFACTOR = 85)");
        else
        {
            Create.Index("IX_HighVolumeTable_TransactionDate")
                .OnTable("HighVolumeTable")
                .OnColumn("TransactionDate");
        }
    }

    public override void Down()
    {
        Delete.Table("HighVolumeTable");
    }
}
```

### 3. Testing Index Effectiveness

```csharp
public class IndexTesting : Migration
{
    public override void Up()
    {
        // Create test data to validate index effectiveness
        Create.Table("TestTable")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("SearchColumn").AsString(100).NotNullable()
            .WithColumn("FilterColumn").AsString(50).NotNullable()
            .WithColumn("SortColumn").AsDateTime().NotNullable();
            
        // Create index to test
        Create.Index("IX_TestTable_Search_Filter_Sort")
            .OnTable("TestTable")
            .OnColumn("SearchColumn")
            .OnColumn("FilterColumn")
            .OnColumn("SortColumn");
            
        // Insert sample data for testing
        for (int i = 1; i <= 1000; i++)
        {
            Insert.IntoTable("TestTable")
                .Row(new
                {
                    SearchColumn = $"Test{i:D4}",
                    FilterColumn = i % 10 == 0 ? "Special" : "Regular",
                    SortColumn = DateTime.Now.AddDays(-i)
                });
        }
    }

    public override void Down()
    {
        Delete.Table("TestTable");
    }
}
```

## Troubleshooting Index Issues

### Common Problems and Solutions

```csharp
public class IndexTroubleshooting : Migration
{
    public override void Up()
    {
        // Problem: Duplicate indexes
        // Solution: Check for existing indexes before creating
        if (!Schema.Table("Users").Index("IX_Users_Email").Exists())
        {
            Create.Index("IX_Users_Email")
                .OnTable("Users")
                .OnColumn("Email");
        }
        
        // Problem: Too many indexes on frequently updated tables
        // Solution: Balance read performance vs write performance
        // Create only essential indexes on high-write tables
        
        // Problem: Unused indexes consuming space
        // Solution: Regular index usage monitoring and cleanup
    }

    public override void Down()
    {
        if (Schema.Table("Users").Index("IX_Users_Email").Exists())
        {
            Delete.Index("IX_Users_Email").OnTable("Users");
        }
    }
}
```

### Index Size and Performance Monitoring

```csharp
public class IndexPerformanceMonitoring : Migration
{
    public override void Up()
    {
            IfDatabase(ProcessorIdConstants.SqlServer).Execute.Sql(@"
                -- Index size monitoring query:
                -- SELECT 
                --     i.name AS IndexName,
                --     SUM(a.total_pages) * 8 AS IndexSizeKB,
                --     SUM(a.used_pages) * 8 AS IndexUsedKB
                -- FROM sys.indexes i
                -- INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                -- INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                -- WHERE OBJECT_NAME(i.object_id) = 'YourTableName'
                -- GROUP BY i.name
                -- ORDER BY IndexSizeKB DESC
                ");
    }

    public override void Down()
    {
        // No rollback needed for documentation
    }
}
```

## Advanced Index Operations with Execute.Sql

For comprehensive examples of advanced indexing operations using Execute.Sql including:
- Database-specific index types and options
- Complex partial and filtered indexes
- Index maintenance and optimization
- Custom index creation with SQL

See: [Raw SQL (Scripts & Helpers)](../raw-sql-scripts.md)

## See Also

- [Creating Tables](../operations/create-tables.md)
- [Altering Tables](../operations/alter-tables.md)
- [Managing Columns](../managing-columns.md)
- [Working with Foreign Keys](../working-with-foreign-keys.md)
- [Best Practices](../advanced/best-practices.md)
- [Database Provider Specific Features](../providers/)