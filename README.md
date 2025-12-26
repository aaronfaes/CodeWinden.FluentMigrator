# CodeWinden.FluentMigrator

A minimal, opinionated wrapper around FluentMigrator for SQL Server database migrations that simplifies configuration and automates database schema changes.

## Packages

### CodeWinden.FluentMigrator.SqlServer

A minimal, opinionated wrapper around FluentMigrator for SQL Server database migrations that simplifies configuration and automates database schema changes.

**Key Features:**
- Fluent builder pattern for simple configuration
- Automatic migration discovery from your assembly
- Built-in support for multiple configuration sources (appsettings.json, environment variables, command-line args)
- Version-based migration control (migrate up, rollback, refresh)
- Configurable command timeouts and custom version table support

[**View Documentation →**](src/CodeWinden.FluentMigrator.SqlServer/README.md)

```bash
dotnet add package CodeWinden.FluentMigrator.SqlServer
```

---

## License

MIT License - see [LICENSE](LICENSE.md) for details
