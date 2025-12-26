using System.Data.Common;
using System.Reflection;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeWinden.FluentMigrator.SqlServer;

/// <summary>
/// The migrator options
/// </summary>
public record MigratorOptions
{
    /// <summary>
    /// The service collection used for creating the Migrator's service provider
    /// </summary>
    public required IServiceCollection ServiceCollection { get; init; }
    /// <summary>
    /// The assembly that contains the migrations
    /// </summary>
    public required Assembly AssemblyWithMigrations { get; init; }
    /// <summary>
    /// The database connection string
    /// </summary>
    public required string ConnectionString { get; init; }
    /// <summary>
    /// Indicates whether to refresh the database before applying migrations
    /// </summary>
    public bool Refresh { get; init; }
    /// <summary>
    /// The version to roll back to, if specified
    /// </summary>
    public long? RollbackToVersion { get; init; }
    /// <summary>
    /// The version to migrate to, if specified
    /// </summary>
    public long? MigrateToVersion { get; init; }
    /// <summary>
    /// The command timeout for database operations
    /// </summary>
    public required TimeSpan Timeout { get; init; }
    /// <summary>
    /// The version table metadata instance
    /// </summary>
    public required IVersionTableMetaData VersionTableInstance { get; init; }
}

/// <summary>
/// Builder for <see cref="MigratorOptions"/>. You can use this to fluently configure the options.
/// </summary>
public class MigratorOptionsBuilder
{
    /// <summary>
    /// The service collection used for creating the Migrator's service provider
    /// </summary>
    private IServiceCollection _serviceCollection = new ServiceCollection();
    /// <summary>
    /// The assembly that contains the migrations
    /// </summary>
    private Assembly? _assemblyWithMigrations;
    /// <summary>
    /// The database connection string
    /// </summary>
    private string? _connectionString;
    /// <summary>
    /// The configuration key to use for retrieving the connection string
    /// </summary>
    private string _connectionStringConfigurationKey = "Migrator:ConnectionString";
    /// <summary>
    /// The command timeout for database operations
    /// </summary>
    private TimeSpan? _timeout;
    /// <summary>
    /// The arguments to use for configuration
    /// </summary>
    private IEnumerable<string>? _arguments;
    /// <summary>
    /// The environment name for loading environment-specific configuration appsettings json file
    /// </summary>
    private string? _environment;
    /// <summary>
    /// The version table metadata instance
    /// </summary>
    private IVersionTableMetaData? _versionTableInstance;

    /// <summary>
    /// Sets the service collection to be used for creating the Migrator's service provider.
    /// </summary>
    /// <param name="serviceCollection">The service collection to be used for creating the Migrator's service provider.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetServiceCollection(IServiceCollection serviceCollection)
    {
        _serviceCollection = serviceCollection;
        return this;
    }

    /// <summary>
    /// Sets the assembly that contains the migrations by specifying a type from that assembly.
    /// </summary>
    /// <typeparam name="T">The type from the assembly that contains the migrations.</typeparam>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetAssemblyWithMigrations<T>()
    {
        return SetAssemblyWithMigrations(typeof(T).Assembly);
    }

    /// <summary>
    /// Sets the assembly that contains the migrations.
    /// </summary>
    /// <param name="assembly">The assembly that contains the migrations.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetAssemblyWithMigrations(Assembly assembly)
    {
        _assemblyWithMigrations = assembly;
        return this;
    }

    /// <summary>
    /// Sets the database connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Sets the configuration key to use for retrieving the connection string.
    /// </summary>
    /// <param name="configurationKey">The configuration key to use for retrieving the connection string.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetConnectionStringConfigurationKey(string configurationKey)
    {
        _connectionStringConfigurationKey = configurationKey;
        return this;
    }

    /// <summary>
    /// Sets the environment name for loading environment-specific configuration appsettings json file.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetEnvironment(string environment)
    {
        _environment = environment;
        return this;
    }

    /// <summary>
    /// Sets the command timeout for database operations.
    /// </summary>
    /// <param name="timeout">The command timeout duration.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the arguments to use for configuration.
    /// 
    /// Can be passed down from command line arguments.
    /// </summary>
    /// <param name="arguments">The arguments to use for configuration.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetArguments(IEnumerable<string> arguments)
    {
        _arguments = arguments;
        return this;
    }

    /// <summary>
    /// Sets the version table metadata instance.
    /// </summary>
    /// <param name="versionTableInstance">The version table metadata instance.</param>
    /// <returns>The current instance of <see cref="MigratorOptionsBuilder"/> for chaining.</returns>
    public MigratorOptionsBuilder SetVersionTableInstance(IVersionTableMetaData versionTableInstance)
    {
        _versionTableInstance = versionTableInstance;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="MigratorOptions"/> instance based on the configured settings.
    /// 
    /// Loads the configuration from various sources:
    /// - appsettings.json
    /// - appsettings.{Environment}.json (if environment is set)
    /// - Environment Variables
    /// - Command Line Arguments (if provided)
    /// - Manually set properties
    /// </summary>
    /// <returns>The configured <see cref="MigratorOptions"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public MigratorOptions Build()
    {
        // Load Configuration
        var configuration = LoadConfiguration();

        // Add To Service Collection
        _serviceCollection.AddSingleton(configuration);

        // Ensure Assembly with Migrations is provided
        ArgumentNullException.ThrowIfNull(_assemblyWithMigrations, "Assembly with migrations must be provided.");

        // Get Connection String from Configuration if not set directly
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _connectionString = configuration.GetValue<string>(_connectionStringConfigurationKey);
        }

        // Ensure Connection String is provided
        ArgumentNullException.ThrowIfNull(_connectionString, "Connection string must be provided either directly or via configuration.");

        // Set default Timeout if not provided
        if (!_timeout.HasValue)
        {
            var timeoutSeconds = configuration.GetValue<int?>("Migrator:Timeout");
            _timeout = TimeSpan.FromSeconds(timeoutSeconds ?? 60);
        }

        // Check that only RollbackToVersion or MigrateToVersion is set
        var rollbackToVersion = configuration.GetValue<long?>("Migrator:RollbackToVersion");
        var migrateToVersion = configuration.GetValue<long?>("Migrator:MigrateToVersion");

        if (rollbackToVersion.HasValue && migrateToVersion.HasValue)
        {
            throw new ArgumentException("Only one of RollbackToVersion or MigrateToVersion can be set.");
        }

        // Create the options and return
        return new MigratorOptions
        {
            ServiceCollection = _serviceCollection,
            AssemblyWithMigrations = _assemblyWithMigrations,
            ConnectionString = _connectionString,
            Timeout = _timeout.Value,
            Refresh = configuration.GetValue<bool?>("Migrator:Refresh") ?? false,
            RollbackToVersion = rollbackToVersion,
            MigrateToVersion = migrateToVersion,
            VersionTableInstance = _versionTableInstance ?? new MigratorVersionTableMetaData()
        };
    }

    /// <summary>
    /// Loads the configuration from various sources:
    /// </summary>
    /// <returns>The built configuration.</returns>
    private IConfiguration LoadConfiguration()
    {
        // Create Configuration Builder and add sources
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        ;

        // If environment is set, add environment specific JSON file
        if (!string.IsNullOrWhiteSpace(_environment))
        {
            builder = builder.AddJsonFile($"appsettings.{_environment}.json", optional: true, reloadOnChange: true);
        }

        // Always add Environment Variables
        builder = builder.AddEnvironmentVariables();

        // If arguments are provided, add Command Line configuration source
        if (_arguments != null && _arguments.Any())
        {
            builder = builder.AddCommandLine(source =>
                {
                    source.Args = _arguments;
                    source.SwitchMappings = new Dictionary<string, string>()
                    {
                        { "--ConnectionString", _connectionStringConfigurationKey },
                        { "--Timeout", "Migrator:Timeout"},
                        { "--RollbackToVersion", "Migrator:RollbackToVersion" },
                        { "--MigrateToVersion", "Migrator:MigrateToVersion" },
                        { "--Refresh", "Migrator:Refresh" }
                    };
                })
            ;
        }

        // Build the configuration
        return builder.Build();
    }
}
