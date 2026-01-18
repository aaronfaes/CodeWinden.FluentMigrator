namespace CodeWinden.FluentMigrator.Tests.TestMigrations;

/// <summary>
/// Example custom service interface for testing dependency injection in migrations
/// </summary>
public interface ICustomMigrationService
{
    /// <summary>
    /// Logs that a migration was executed
    /// </summary>
    /// <param name="migrationName">The name of the migration</param>
    void LogMigrationExecution(string migrationName);

    /// <summary>
    /// Gets the list of executed migrations tracked by this service
    /// </summary>
    IReadOnlyList<string> ExecutedMigrations { get; }
}

/// <summary>
/// Test implementation of custom migration service
/// </summary>
public class CustomMigrationService : ICustomMigrationService
{
    private readonly List<string> _executedMigrations = new();

    public IReadOnlyList<string> ExecutedMigrations => _executedMigrations.AsReadOnly();

    public void LogMigrationExecution(string migrationName)
    {
        _executedMigrations.Add(migrationName);
    }
}
