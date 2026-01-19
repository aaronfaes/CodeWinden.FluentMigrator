using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeWinden.FluentMigrator.SqlServer;

/// <summary>
/// Database migrator using FluentMigrator
/// 
/// Use the <see cref="Create"/> method to instantiate and configure the migrator.
/// </summary>
public class Migrator
{
    /// <summary>
    /// The migrator options
    /// </summary>
    private readonly MigratorOptions _options;
    /// <summary>
    /// The logger instance
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Migrator"/> class.
    /// </summary>
    /// <param name="options">The migrator options.</param>
    /// <param name="logger">The logger instance.</param>
    private Migrator(MigratorOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Executes the database migrations.
    /// </summary>
    public void ExecuteMigrations()
    {
        // Configure the FluentMigrator services
        var serviceProvider = CreateServices();

        // Execute the migrations
        ExecuteMigrations(serviceProvider);
    }

    /// <summary>
    /// Executes the database migrations.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    private void ExecuteMigrations(IServiceProvider serviceProvider)
    {
        // Make sure to log the start of the migration with the applicable settings
        LogInformation();

        // Create a scope to run the migrations
        using (var scope = serviceProvider.CreateScope())
        {
            // Get the migration runner
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            // List all migrations
            runner.ListMigrations();

            // Check if the migrator needs to rollback to a specific version
            if (_options.RollbackToVersion.HasValue)
            {
                _logger.LogInformation("Start 'Rollback' to version {Version} of the database.", _options.RollbackToVersion);
                runner.MigrateDown(_options.RollbackToVersion.Value);
                _logger.LogInformation("Finished 'Rollback' to version {Version} of the database.", _options.RollbackToVersion);
                return;
            }

            // Check if the migrator needs to Migrate to a specific version
            if (_options.MigrateToVersion.HasValue)
            {
                // Check if migrations need to be applied.
                if (!runner.HasMigrationsToApplyUp(_options.MigrateToVersion))
                {
                    _logger.LogInformation("No migrations found to execute.");
                    return;
                }

                _logger.LogInformation("Start 'Migrate' to version {Version} of the database.", _options.MigrateToVersion);
                runner.MigrateUp(_options.MigrateToVersion.Value);
                _logger.LogInformation("Finished 'Migrate' to version {Version} of the database.", _options.MigrateToVersion);
                return;
            }

            // Check if migrations need to be applied.
            if (!runner.HasMigrationsToApplyUp() && !_options.Refresh)
            {
                _logger.LogInformation("No migrations found to execute.");
                return;
            }

            // Check if we want to have a fully clean database, this will rollback all migrations
            if (_options.Refresh)
            {
                _logger.LogInformation("Start 'Refreshing' the database. !Note: All data will be lost!");
                runner.MigrateDown(0);
                _logger.LogInformation("Finished 'Refreshing' the database.");
            }

            // Migrate to the latest version
            _logger.LogInformation("Start 'Migrating' the database.");
            runner.MigrateUp();
            _logger.LogInformation("Finished 'Migrating' the database.");
        }
    }

    /// <summary>
    /// Logs the migrator information with all settings.
    /// 
    /// The connection string will have its sensitive information removed.
    /// </summary>
    private void LogInformation()
    {
        _logger.LogInformation(
@"Starting database migration with the following settings:
- Connection String: {ConnectionString}
- Assembly with Migrations: {AssemblyName}
- Timeout: {Timeout}
- Refresh: {Refresh}
- Rollback To Version: {RollBackToVersion}
- Migrate To Version: {MigrateToVersion}",
            _options.ConnectionString.RemoveConnectionStringSecrets(),
            _options.AssemblyWithMigrations.FullName,
            _options.Timeout,
            _options.Refresh,
            _options.RollbackToVersion?.ToString() ?? "N/A",
            _options.MigrateToVersion?.ToString() ?? "N/A"
        );
    }

    /// <summary>
    /// Creates the service provider for FluentMigrator.
    /// </summary>
    /// <returns>The service provider.</returns>
    private IServiceProvider CreateServices()
    {
        _options.ServiceCollection
            .AddLogging(options => options.AddFluentMigratorConsole())
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .WithVersionTable(_options.VersionTableInstance)
                .AddSqlServer()
                .WithGlobalConnectionString(_options.ConnectionString)
                .ScanIn(_options.AssemblyWithMigrations).For.Migrations()
                .WithGlobalCommandTimeout(_options.Timeout)
            )
        ;
        return _options.ServiceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Migrator"/> with the specified configuration.
    /// </summary>
    /// <param name="optionsAction">The action to configure the migrator options.</param>
    /// <returns>A new instance of the <see cref="Migrator"/>.</returns>
    public static Migrator Create(Action<MigratorOptionsBuilder> optionsAction)
    {
        // Create Logger
        var logger = GetLogger();

        try
        {
            // Create new Builder
            var optionsBuilder = new MigratorOptionsBuilder();

            // Apply user configuration
            optionsAction(optionsBuilder);

            // Build the options
            var options = optionsBuilder.Build();

            // Return new instance of Migrator
            return new Migrator(options, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating the Migrator.");

            throw;
        }
    }

    /// <summary>
    /// Gets the logger instance for the migrator.
    /// </summary>
    /// <returns>A logger instance.</returns>
    private static ILogger GetLogger()
    {
        using (var logFactory = LoggerFactory.Create((builder) => builder.AddFluentMigratorConsole()))
        {
            return logFactory.CreateLogger<Migrator>();
        }
    }
}
