# CodeWinden.FluentMigrator.SqlServer

A minimal, opinionated wrapper around [FluentMigrator](https://fluentmigrator.github.io/) for SQL Server database migrations that simplifies configuration and automates database schema changes.

## Features

- **Zero boilerplate** - Fluent builder pattern for simple configuration
- **Automatic discovery** - Scans assembly for FluentMigrator migrations
- **Flexible configuration** - Load settings from appsettings.json, environment variables, or command-line args
- **Version control** - Migrate up, rollback, or refresh database schema
- **Configurable timeouts** - Set command timeout for long-running operations
- **Custom version tables** - Override default migration history tracking
- **Integrated logging** - Uses FluentMigrator's built-in console output

## Installation

```bash
dotnet add package CodeWinden.FluentMigrator.SqlServer
```

## Quick Start

Configure the migrator with your assembly and connection string, then execute migrations.

## Usage

### Creating Migrations

Create migration classes using FluentMigrator's standard syntax. See the [FluentMigrator documentation](https://fluentmigrator.github.io/articles/intro.html) for complete migration syntax reference.

### Configuring the Migrator

Use the fluent builder API to configure migration behavior. Required settings include the assembly containing migrations and the connection string. Optional settings include command timeout, custom service collection, and version table customization.

### Running and configuration from Command-Line Arguments

Pass command-line arguments for dynamic configuration using the `SetArguments()` method.

```csharp
using CodeWinden.FluentMigrator.SqlServer;

// Configure migrator with command-line arguments
var migrator = Migrator.Create(options => options
    .SetAssemblyWithMigrations<Program>()
    .SetArguments(args)
);

migrator.ExecuteMigrations();
```

Run with arguments:
```bash
dotnet run --ConnectionString "Server=localhost;Database=MyDb;Integrated Security=true;" --Timeout 120
```

Available switches:
- `--ConnectionString` - Database connection string
- `--Timeout` - Command timeout in seconds
- `--MigrateToVersion` - Migrate to specific version
- `--RollbackToVersion` - Rollback to specific version
- `--Refresh` - Drop all migrations and reapply (⚠️ destroys data)

### Migrate to Specific Version

Set `MigrateToVersion` via appsettings.json or command-line arguments to migrate up to a specific version.

### Rollback to Specific Version

Set `RollbackToVersion` via appsettings.json or command-line arguments to rollback to a specific version.

### Refresh Database (⚠️ Destroys All Data)

Set `Refresh` to true via appsettings.json or command-line arguments to drop all migrations and reapply them from scratch.

> **Warning:** Refresh mode executes all `Down()` methods in reverse order, then all `Up()` methods. All existing data will be lost.

### Custom Version Table

Customize the migration version tracking table by implementing `IVersionTableMetaData` and passing it to `SetVersionTableInstance()`.

## API Reference

### Core Classes

| Class | Purpose |
|-------|---------|
| `Migrator` | Main class for executing database migrations |
| `MigratorOptions` | Configuration record containing all migrator settings |
| `MigratorOptionsBuilder` | Fluent builder for configuring migrator options |
| `MigratorVersionTableMetaData` | Default version table configuration (schema: `Migrations`, table: `VersionInfo`) |

### MigratorOptionsBuilder Methods

| Method | Description |
|--------|-------------|
| `SetAssemblyWithMigrations<T>()` | Set assembly containing migrations using a type from that assembly |
| `SetAssemblyWithMigrations(Assembly)` | Set assembly containing migrations directly |
| `SetConnectionString(string)` | Set database connection string |
| `SetConnectionStringConfigurationKey(string)` | Set configuration key for connection string (default: `Migrator:ConnectionString`) |
| `SetEnvironment(string)` | Set environment name for loading appsettings.{Environment}.json |
| `SetTimeout(TimeSpan)` | Set command timeout for database operations (default: 60 seconds) |
| `SetArguments(IEnumerable<string>)` | Set command-line arguments for configuration |
| `SetServiceCollection(IServiceCollection)` | Provide custom service collection |
| `SetVersionTableInstance(IVersionTableMetaData)` | Customize version table schema |

### MigratorOptions Properties

| Property | Description |
|----------|-------------|
| `ServiceCollection` | Service collection for dependency injection |
| `AssemblyWithMigrations` | Assembly containing migration classes |
| `ConnectionString` | Database connection string |
| `Timeout` | Command timeout for operations |
| `Refresh` | Whether to drop and reapply all migrations |
| `RollbackToVersion` | Target version for rollback |
| `MigrateToVersion` | Target version for migration |
| `VersionTableInstance` | Version table metadata instance |

## Configuration Priority

Configuration is loaded from multiple sources in the following order (later sources override earlier):

1. appsettings.json
2. appsettings.{Environment}.json (if environment specified)
3. Environment variables
4. Command-line arguments (if provided)
5. Builder methods (e.g., `SetConnectionString()`, `SetTimeout()`)

## Best Practices

**Connection strings:**
- Store sensitive connection strings in environment-specific configuration files or environment variables
- Use `appsettings.Development.json` for local development
- Never commit production connection strings to source control
- Load from environment-specific config using `SetEnvironment()`

**Timeout settings:**
- Increase timeout for migrations with large data operations using `SetTimeout()`
- Default timeout is 60 seconds

**FluentMigrator syntax:**

This library handles configuration and execution. For migration syntax (tables, columns, indexes, data operations), see the [FluentMigrator documentation](https://fluentmigrator.github.io/articles/intro.html):
- [Migration syntax guide](https://fluentmigrator.github.io/articles/migration-example.html)
- [Fluent interface reference](https://fluentmigrator.github.io/articles/fluent-interface.html)
- [Raw SQL execution](https://fluentmigrator.github.io/articles/raw-sql.html)

## License

MIT License - see [LICENSE](LICENSE.md) for details