# Installation

FluentMigrator can be installed in several ways depending on your project type and preferences.

## Package Installation

### Core Packages

All projects need these base packages:

```xml
<PackageReference Include="FluentMigrator" Version="6.2.0" />
<PackageReference Include="FluentMigrator.Runner" Version="6.2.0" />
```

### Database Provider Packages

Choose the package for your database provider:

#### SQL Server
```xml
<PackageReference Include="FluentMigrator.Runner.SqlServer" Version="6.2.0" />
```

#### PostgreSQL
```xml
<PackageReference Include="FluentMigrator.Runner.Postgres" Version="6.2.0" />
```

#### MySQL / MariaDB
```xml
<PackageReference Include="FluentMigrator.Runner.MySql" Version="6.2.0" />
```

#### SQLite
```xml
<PackageReference Include="FluentMigrator.Runner.SQLite" Version="6.2.0" />
```

#### Oracle
```xml
<PackageReference Include="FluentMigrator.Runner.Oracle" Version="6.2.0" />
```

#### Firebird
```xml
<PackageReference Include="FluentMigrator.Runner.Firebird" Version="6.2.0" />
```

#### Other Providers
```xml
<!-- IBM DB2 -->
<PackageReference Include="FluentMigrator.Runner.Db2" Version="6.2.0" />

<!-- SAP HANA -->
<PackageReference Include="FluentMigrator.Runner.Hana" Version="6.2.0" />

<!-- Snowflake -->
<PackageReference Include="FluentMigrator.Runner.Snowflake" Version="6.2.0" />

<!-- Amazon Redshift -->
<PackageReference Include="FluentMigrator.Runner.Redshift" Version="6.2.0" />
```

## Installation Methods

### .NET CLI
```bash
dotnet add package FluentMigrator
dotnet add package FluentMigrator.Runner
dotnet add package FluentMigrator.Runner.SqlServer
```

### Package Manager Console (Visual Studio)
```powershell
Install-Package FluentMigrator
Install-Package FluentMigrator.Runner
Install-Package FluentMigrator.Runner.SqlServer
```

### Package Manager UI
1. Right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages"
3. Search for "FluentMigrator" and install the required packages

## Command Line Tools

### .NET Tool
Install the FluentMigrator .NET tool for command-line usage:

```bash
dotnet tool install -g FluentMigrator.DotNet.Cli
```

Usage:
```bash
dotnet fm migrate -p sqlserver -c "Server=.;Database=MyApp;Trusted_Connection=true;" -a "MyApp.dll"
```

### MSBuild Integration
For MSBuild integration, add the MSBuild package:

```xml
<PackageReference Include="FluentMigrator.MSBuild" Version="6.2.0" />
```

## Project Templates

### Console Application Template

Create a new console application for running migrations:

```bash
dotnet new console -n MyApp.Migrations
cd MyApp.Migrations
dotnet add package FluentMigrator
dotnet add package FluentMigrator.Runner
dotnet add package FluentMigrator.Runner.SqlServer
```

### Web Application Integration

For ASP.NET Core applications, add migration support to your existing web project:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator" Version="6.2.0" />
    <PackageReference Include="FluentMigrator.Runner" Version="6.2.0" />
    <PackageReference Include="FluentMigrator.Runner.SqlServer" Version="6.2.0" />
  </ItemGroup>
</Project>
```

## Framework Support

FluentMigrator supports multiple .NET frameworks:

- **.NET 8.0** ✅ (Recommended)
- **.NET 7.0** ✅
- **.NET 6.0** ✅ (LTS)
- **.NET Standard 2.0** ✅
- **.NET Framework 4.8** ✅

## Version Compatibility

| FluentMigrator Version | .NET Version | Status |
|----------------------|--------------|---------|
| 6.2.x | .NET 6+ | Current |
| 5.x | .NET 5+ | Supported |
| 3.x | .NET Core 2+ | Legacy |

## Verification

After installation, verify that FluentMigrator is properly installed by creating a simple migration:

```csharp
using FluentMigrator;

[Migration(1)]
public class TestMigration : Migration
{
    public override void Up()
    {
        // Migration code here
    }

    public override void Down()
    {
        // Rollback code here
    }
}
```

If the code compiles without errors, FluentMigrator is correctly installed.

## Common Installation Issues

### Package Conflicts
If you encounter package conflicts, ensure all FluentMigrator packages are the same version:

```xml
<PackageReference Include="FluentMigrator" Version="6.2.0" />
<PackageReference Include="FluentMigrator.Runner" Version="6.2.0" />
<PackageReference Include="FluentMigrator.Runner.SqlServer" Version="6.2.0" />
```

### Missing Database Provider
Error: "No database provider registered"

Solution: Install the appropriate database provider package for your database.

### Assembly Loading Issues
If you get assembly loading errors, ensure your target framework is compatible and all dependencies are properly installed.

## Next Steps

Once FluentMigrator is installed, proceed to:
- [Quick Start Guide](./quick-start.md) - Create your first migration
- [Creating Tables](./operations/create-tables.md) - Learn the table creation API
- [Database Providers](./providers/sql-server.md) - Provider-specific configuration