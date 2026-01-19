using CodeWinden.FluentMigrator.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeWinden.FluentMigrator.Tests;

public partial class MigratorOptionsBuilderTests
{
    #region SetServiceCollection Tests

    [Fact]
    public void SetServiceCollection_WithValidServiceCollection_UsesProvidedServiceCollection()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<TestService>();

        // Act
        builder.SetServiceCollection(serviceCollection)
            .SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;Database=Test;");
        var options = builder.Build();

        // Assert
        var serviceProvider = options.ServiceCollection.BuildServiceProvider();
        var testService = serviceProvider.GetService<TestService>();
        Assert.NotNull(testService);
    }

    #endregion

    #region SetAssemblyWithMigrations Tests

    [Fact]
    public void SetAssemblyWithMigrations_WithType_SetsAssemblyCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedAssembly = typeof(MigratorOptionsBuilderTests).Assembly;

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;Database=Test;");
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedAssembly, options.AssemblyWithMigrations);
    }

    [Fact]
    public void SetAssemblyWithMigrations_WithAssembly_SetsAssemblyCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedAssembly = typeof(MigratorOptionsBuilderTests).Assembly;

        // Act
        builder.SetAssemblyWithMigrations(expectedAssembly)
            .SetConnectionString("Server=localhost;Database=Test;");
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedAssembly, options.AssemblyWithMigrations);
    }

    #endregion

    #region SetConnectionString Tests

    [Fact]
    public void SetConnectionString_WithValidString_SetsConnectionStringCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var connectionString = "Server=localhost;Database=TestDb;User=sa;Password=Pass123;";

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString(connectionString);
        var options = builder.Build();

        // Assert
        Assert.Equal(connectionString, options.ConnectionString);
    }

    #endregion

    #region SetConnectionStringConfigurationKey Tests

    [Fact]
    public void SetConnectionStringConfigurationKey_WithCustomKey_UsesCustomKeyForConfiguration()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var customKey = "CustomConnectionString";
        var expectedConnectionString = "Server=localhost;Database=Custom;";

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionStringConfigurationKey(customKey)
            .SetArguments(new[] { $"--{customKey}={expectedConnectionString}" });
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedConnectionString, options.ConnectionString);
    }

    #endregion

    #region SetEnvironment Tests

    [Fact]
    public void SetEnvironment_WithDevelopmentEnvironment_LoadsEnvironmentSpecificConfiguration()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var environment = "Development";

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetEnvironment(environment);
        var options = builder.Build();

        // Assert - Should load from appsettings.Development.json
        Assert.Equal("Server=localhost;Database=DevelopmentDatabase;", options.ConnectionString);
        Assert.Equal(TimeSpan.FromSeconds(120), options.Timeout);

        // Verify configuration is available in service collection
        var serviceProvider = options.ServiceCollection.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        Assert.NotNull(configuration);
        Assert.Equal("Server=localhost;Database=DevelopmentDatabase;", configuration["Migrator:ConnectionString"]);
    }

    [Fact]
    public void SetEnvironment_WithProductionEnvironment_LoadsEnvironmentSpecificConfiguration()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var environment = "Production";

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetEnvironment(environment);
        var options = builder.Build();

        // Assert - Should load from appsettings.Production.json
        Assert.Equal("Server=prodserver;Database=ProductionDatabase;", options.ConnectionString);
        Assert.Equal(TimeSpan.FromSeconds(300), options.Timeout);

        // Verify configuration is available in service collection
        var serviceProvider = options.ServiceCollection.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        Assert.NotNull(configuration);
        Assert.Equal("Server=prodserver;Database=ProductionDatabase;", configuration["Migrator:ConnectionString"]);
    }

    [Fact]
    public void SetEnvironment_WithoutEnvironment_LoadsDefaultConfiguration()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();

        // Act - No environment set, should load only appsettings.json
        builder
            .SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;Database=Test;")
        ;

        var options = builder.Build();

        // Assert - Should load from appsettings.json (default)
        Assert.Equal(TimeSpan.FromSeconds(90), options.Timeout);
    }

    #endregion

    #region SetTimeout Tests

    [Fact]
    public void SetTimeout_WithValidTimeout_SetsTimeoutCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var timeout = TimeSpan.FromSeconds(120);

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;Database=Test;")
            .SetTimeout(timeout);
        var options = builder.Build();

        // Assert
        Assert.Equal(timeout, options.Timeout);
    }

    #endregion

    #region SetArguments Tests

    [Fact]
    public void SetArguments_WithConnectionStringArgument_OverridesConnectionString()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedConnectionString = "Server=localhost;Database=FromArgs;";
        var arguments = new[] { "--ConnectionString", expectedConnectionString };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedConnectionString, options.ConnectionString);
    }

    [Fact]
    public void SetArguments_WithTimeoutArgument_OverridesTimeout()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedTimeoutSeconds = 180;
        var arguments = new[] { "--ConnectionString", "Server=localhost;", "--Timeout", expectedTimeoutSeconds.ToString() };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedTimeoutSeconds), options.Timeout);
    }

    #endregion

    #region SetVersionTableInstance Tests

    [Fact]
    public void SetVersionTableInstance_WithValidInstance_SetsInstanceCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var versionTableInstance = new TestVersionTableMetaData();

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;Database=Test;")
            .SetVersionTableInstance(versionTableInstance);
        var options = builder.Build();

        // Assert
        Assert.Same(versionTableInstance, options.VersionTableInstance);
    }

    #endregion

    #region Build Method Tests

    [Fact]
    public void Build_WithMinimalValidConfiguration_CreatesOptionsSuccessfully()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var assembly = typeof(MigratorOptionsBuilderTests).Assembly;
        var connectionString = "Server=localhost;Database=Test;";

        // Act
        builder.SetAssemblyWithMigrations(assembly)
            .SetConnectionString(connectionString);
        var options = builder.Build();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(assembly, options.AssemblyWithMigrations);
        Assert.Equal(connectionString, options.ConnectionString);
        Assert.NotNull(options.ServiceCollection);
        Assert.NotNull(options.VersionTableInstance);
        Assert.Equal(TimeSpan.FromSeconds(90), options.Timeout); // Default timeout
    }

    [Fact]
    public void Build_WithoutAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        builder.SetConnectionString("Server=localhost;Database=Test;");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => builder.Build());
        Assert.Contains("Assembly with migrations must be provided", exception.Message);
    }

    [Fact]
    public void Build_WithoutConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => builder.Build());
        Assert.Contains("Connection string must be provided either directly or via configuration.", exception.Message);
    }

    [Fact]
    public void Build_WithBothRollbackAndMigrateVersion_ThrowsArgumentException()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var arguments = new[]
        {
            "--ConnectionString", "Server=localhost;",
            "--RollbackToVersion", "1",
            "--MigrateToVersion", "2"
        };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);

        // Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("Only one of RollbackToVersion or MigrateToVersion can be set", exception.Message);
    }

    [Fact]
    public void Build_WithRefreshArgument_SetsRefreshToTrue()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var arguments = new[]
        {
            "--ConnectionString", "Server=localhost;",
            "--Refresh", "true"
        };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert
        Assert.True(options.Refresh);
    }

    [Fact]
    public void Build_WithoutRefreshArgument_SetsRefreshToFalse()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;");
        var options = builder.Build();

        // Assert
        Assert.False(options.Refresh);
    }

    [Fact]
    public void Build_WithRollbackToVersionArgument_SetsRollbackToVersion()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedVersion = 5L;
        var arguments = new[]
        {
            "--ConnectionString", "Server=localhost;",
            "--RollbackToVersion", expectedVersion.ToString()
        };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedVersion, options.RollbackToVersion);
        Assert.Null(options.MigrateToVersion);
    }

    [Fact]
    public void Build_WithMigrateToVersionArgument_SetsMigrateToVersion()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var expectedVersion = 10L;
        var arguments = new[]
        {
            "--ConnectionString", "Server=localhost;",
            "--MigrateToVersion", expectedVersion.ToString()
        };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert
        Assert.Equal(expectedVersion, options.MigrateToVersion);
        Assert.Null(options.RollbackToVersion);
    }

    [Fact]
    public void Build_AddsConfigurationToServiceCollection()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString("Server=localhost;");
        var options = builder.Build();

        // Assert
        var serviceProvider = options.ServiceCollection.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        Assert.NotNull(configuration);
    }

    [Fact]
    public void Build_FluentApiChaining_WorksCorrectly()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var assembly = typeof(MigratorOptionsBuilderTests).Assembly;
        var connectionString = "Server=localhost;Database=Test;";
        var timeout = TimeSpan.FromSeconds(90);
        var versionTable = new TestVersionTableMetaData();
        var serviceCollection = new ServiceCollection();

        // Act
        var options = builder
            .SetServiceCollection(serviceCollection)
            .SetAssemblyWithMigrations(assembly)
            .SetConnectionString(connectionString)
            .SetTimeout(timeout)
            .SetVersionTableInstance(versionTable)
            .Build();

        // Assert
        Assert.Equal(assembly, options.AssemblyWithMigrations);
        Assert.Equal(connectionString, options.ConnectionString);
        Assert.Equal(timeout, options.Timeout);
        Assert.Same(versionTable, options.VersionTableInstance);
    }

    [Fact]
    public void Build_DirectConnectionStringOverridesConfiguration()
    {
        // Arrange
        var builder = new MigratorOptionsBuilder();
        var directConnectionString = "Server=localhost;Database=Direct;";
        var configConnectionString = "Server=localhost;Database=Config;";
        var arguments = new[] { "--ConnectionString", configConnectionString };

        // Act
        builder.SetAssemblyWithMigrations<MigratorOptionsBuilderTests>()
            .SetConnectionString(directConnectionString)
            .SetArguments(arguments);
        var options = builder.Build();

        // Assert - Direct connection string should take precedence
        Assert.Equal(directConnectionString, options.ConnectionString);
    }

    #endregion

    #region Helper Classes

    private class TestService
    {
    }

    #endregion
}
