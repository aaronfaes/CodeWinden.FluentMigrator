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
- **Dependency injection** - Inject custom services into migrations for complex scenarios

## When to Use This Library

> **Important:** This library is **not** intended to replace [FluentMigrator.Console](https://fluentmigrator.github.io/runners/console.html). For straightforward database migrations without custom dependencies, **use FluentMigrator.Console** - it's the recommended approach.

**Use this library when:**

- You need to **inject custom services** into your migrations (e.g., encryption services, external APIs, business logic)
- Your migrations require **dependency injection** for complex data transformations
- You want to **integrate migrations into your application startup** with custom service configuration
- You need **programmatic control** over migration execution within your application

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

### Injecting Custom Services into Migrations

Inject custom services (e.g., encryption, logging, external APIs) into your migrations using a custom service collection. This is the primary use case for this library over FluentMigrator.Console.

```csharp
// Create a migration that uses the service
[Migration(20251224120000)]
public class EncryptUserEmails : Migration
{
    private readonly IEncryptionService _encryptionService;

    public EncryptUserEmails(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public override void Up()
    {
        // Add encrypted email column
        Alter.Table("Users")
            .AddColumn("EncryptedEmail").AsString(512).Nullable();

        // Migrate existing emails to encrypted format
        Execute.WithConnection((connection, transaction) =>
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "SELECT Id, Email FROM Users WHERE Email IS NOT NULL";

            using var reader = cmd.ExecuteReader();
            var updates = new List<(int Id, string EncryptedEmail)>();

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var email = reader.GetString(1);
                var encrypted = _encryptionService.Encrypt(email);
                updates.Add((id, encrypted));
            }
            reader.Close();

            foreach (var (id, encryptedEmail) in updates)
            {
                cmd.CommandText = $"UPDATE Users SET EncryptedEmail = @Email WHERE Id = @Id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Email", encryptedEmail);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        });

        // Drop old email column
        Delete.Column("Email").FromTable("Users");
    }

    public override void Down()
    {
        // Restore plaintext email column
        Alter.Table("Users")
            .AddColumn("Email").AsString(255).Nullable();

        Execute.WithConnection((connection, transaction) =>
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "SELECT Id, EncryptedEmail FROM Users WHERE EncryptedEmail IS NOT NULL";

            using var reader = cmd.ExecuteReader();
            var updates = new List<(int Id, string Email)>();

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var encryptedEmail = reader.GetString(1);
                var email = _encryptionService.Decrypt(encryptedEmail);
                updates.Add((id, email));
            }
            reader.Close();

            foreach (var (id, email) in updates)
            {
                cmd.CommandText = $"UPDATE Users SET Email = @Email WHERE Id = @Id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        });

        Delete.Column("EncryptedEmail").FromTable("Users");
    }
}

// Configure the migrator with your custom service
var services = new ServiceCollection();
services.AddSingleton<IEncryptionService, AesEncryptionService>();

var migrator = Migrator.Create(options => options
    .SetAssemblyWithMigrations<EncryptUserEmails>()
    .SetConnectionString("Server=localhost;Database=MyDb;Integrated Security=true;")
    .SetServiceCollection(services)
);

migrator.ExecuteMigrations();
```

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

| Class                          | Purpose                                                                          |
| ------------------------------ | -------------------------------------------------------------------------------- |
| `Migrator`                     | Main class for executing database migrations                                     |
| `MigratorOptions`              | Configuration record containing all migrator settings                            |
| `MigratorOptionsBuilder`       | Fluent builder for configuring migrator options                                  |
| `MigratorVersionTableMetaData` | Default version table configuration (schema: `Migrations`, table: `VersionInfo`) |

### MigratorOptionsBuilder Methods

| Method                                           | Description                                                                        |
| ------------------------------------------------ | ---------------------------------------------------------------------------------- |
| `SetAssemblyWithMigrations<T>()`                 | Set assembly containing migrations using a type from that assembly                 |
| `SetAssemblyWithMigrations(Assembly)`            | Set assembly containing migrations directly                                        |
| `SetConnectionString(string)`                    | Set database connection string                                                     |
| `SetConnectionStringConfigurationKey(string)`    | Set configuration key for connection string (default: `Migrator:ConnectionString`) |
| `SetEnvironment(string)`                         | Set environment name for loading appsettings.{Environment}.json                    |
| `SetTimeout(TimeSpan)`                           | Set command timeout for database operations (default: 60 seconds)                  |
| `SetArguments(IEnumerable<string>)`              | Set command-line arguments for configuration                                       |
| `SetServiceCollection(IServiceCollection)`       | Provide custom service collection                                                  |
| `SetVersionTableInstance(IVersionTableMetaData)` | Customize version table schema                                                     |

### MigratorOptions Properties

| Property                 | Description                                 |
| ------------------------ | ------------------------------------------- |
| `ServiceCollection`      | Service collection for dependency injection |
| `AssemblyWithMigrations` | Assembly containing migration classes       |
| `ConnectionString`       | Database connection string                  |
| `Timeout`                | Command timeout for operations              |
| `Refresh`                | Whether to drop and reapply all migrations  |
| `RollbackToVersion`      | Target version for rollback                 |
| `MigrateToVersion`       | Target version for migration                |
| `VersionTableInstance`   | Version table metadata instance             |

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
