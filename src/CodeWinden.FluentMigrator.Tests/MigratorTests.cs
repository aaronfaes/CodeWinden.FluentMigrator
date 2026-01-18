using System.Data;
using CodeWinden.FluentMigrator.SqlServer;
using CodeWinden.FluentMigrator.Tests.TestMigrations;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace CodeWinden.FluentMigrator.Tests;

/// <summary>
/// Tests for Migrator using TestContainers with actual SQL Server database
/// </summary>
public class MigratorTests : IAsyncLifetime
{
    private MsSqlContainer? _msSqlContainer;
    private string _connectionString = string.Empty;
    private readonly ITestOutputHelper _output;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalConsoleOut;

    public MigratorTests(ITestOutputHelper output)
    {
        _output = output;
        _consoleOutput = new StringWriter();
        _originalConsoleOut = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    /// <summary>
    /// Initializes the SQL Server container before running tests
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and start SQL Server container
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();

        await _msSqlContainer.StartAsync();
        _connectionString = _msSqlContainer.GetConnectionString();
    }

    /// <summary>
    /// Disposes the SQL Server container after running tests
    /// </summary>
    public async Task DisposeAsync()
    {
        Console.SetOut(_originalConsoleOut);
        _consoleOutput?.Dispose();

        if (_msSqlContainer != null)
        {
            await _msSqlContainer.DisposeAsync();
        }
    }

    #region Migration Execution Tests

    [Fact]
    public void ExecuteMigrations_WithValidConfiguration_RunsAllMigrationsSuccessfully()
    {
        // Arrange
        _consoleOutput.GetStringBuilder().Clear();

        // Create a custom service to test dependency injection
        var customService = new CustomMigrationService();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService>(customService);

        var migrator = Migrator.Create(options =>
        {
            options.SetServiceCollection(serviceCollection)
                   .SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString);
        });

        // Act
        migrator.ExecuteMigrations();

        // Assert - Verify tables were created
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var usersTableExists = TableExists(connection, "Users");
        var ordersTableExists = TableExists(connection, "Orders");
        var migrationLogTableExists = TableExists(connection, "MigrationLog");
        var versionTableExists = TableExists(connection, "VersionInfo");

        Assert.True(usersTableExists, "Users table should exist after migration");
        Assert.True(ordersTableExists, "Orders table should exist after migration");
        Assert.True(migrationLogTableExists, "MigrationLog table should exist after migration");
        Assert.True(versionTableExists, "VersionInfo table should exist after migration");

        // Verify all 4 migrations were applied
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(4, migrationCount);

        // Verify Users table structure
        var statusColumnExists = ColumnExists(connection, "Users", "Status");
        Assert.True(statusColumnExists, "Status column should exist in Users table");

        // Verify custom service was used via dependency injection
        Assert.Single(customService.ExecutedMigrations);
        Assert.Contains(nameof(TestMigration004_WithCustomService), customService.ExecutedMigrations);

        // Verify stdout output
        var output = _consoleOutput.ToString();
        Assert.Contains("Starting database migration with the following settings:", output);
        Assert.Contains("- Assembly with Migrations: CodeWinden.FluentMigrator.Tests", output);
        Assert.Contains("Start 'Migrating' the database.", output);
        Assert.Contains("1: TestMigration001_CreateUsersTable migrating", output);
        Assert.Contains("CreateTable Users", output);
        Assert.Contains("2: TestMigration002_CreateOrdersTable migrating", output);
        Assert.Contains("CreateForeignKey FK_Orders_Users Orders(UserId) Users(Id)", output);
        Assert.Contains("3: TestMigration003_AddUserStatus migrating", output);
        Assert.Contains("4: TestMigration004_WithCustomService migrating", output);
        Assert.Contains("CreateTable MigrationLog", output);
        Assert.Contains("Finished 'Migrating' the database.", output);
        Assert.Contains("Beginning Transaction", output);
        Assert.Contains("Committing Transaction", output);
        Assert.DoesNotContain("YourStrong!Passw0rd", output);
    }

    [Fact]
    public void ExecuteMigrations_RunningTwice_DoesNotApplyMigrationsAgain()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        // Arrange
        var migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetServiceCollection(serviceCollection);
        });

        // Act - Run migrations twice
        migrator.ExecuteMigrations();

        // Clear output and reinstantiate to simulate a new run
        _consoleOutput.GetStringBuilder().Clear();

        serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetServiceCollection(serviceCollection);
        });

        migrator.ExecuteMigrations();

        // Assert - Should still have exactly 3 migrations
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(4, migrationCount);

        // Verify stdout shows no migrations executed
        var output = _consoleOutput.ToString();
        Assert.Contains("4: TestMigration004_WithCustomService (current)", output);
        Assert.Contains("No migrations found to execute.", output);
        Assert.DoesNotContain("Start 'Migrating' the database.", output);
        Assert.DoesNotContain("Finished 'Migrating' the database.", output);
    }

    #endregion

    #region Migration to Specific Version Tests

    [Fact]
    public void ExecuteMigrations_WithMigrateToVersion_StopsAtSpecifiedVersion()
    {
        // Arrange
        _consoleOutput.GetStringBuilder().Clear();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetServiceCollection(serviceCollection)
                    .SetArguments(["--MigrateToVersion", "2"]); // Only migrate to version 2
        });

        // Act
        migrator.ExecuteMigrations();

        // Assert
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Should have Users and Orders tables (migrations 1 and 2)
        var usersTableExists = TableExists(connection, "Users");
        var ordersTableExists = TableExists(connection, "Orders");
        Assert.True(usersTableExists, "Users table should exist");
        Assert.True(ordersTableExists, "Orders table should exist");

        // Should have only 2 migrations applied
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(2, migrationCount);

        // Status column should NOT exist (that's migration 3)
        var statusColumnExists = ColumnExists(connection, "Users", "Status");
        Assert.False(statusColumnExists, "Status column should not exist when migrating only to version 2");

        // Verify stdout output
        var output = _consoleOutput.ToString();
        Assert.Contains("- Migrate To Version: 2", output);
        Assert.Contains("Start 'Migrate' to version 2 of the database.", output);
        Assert.Contains("1: TestMigration001_CreateUsersTable migrated", output);
        Assert.Contains("2: TestMigration002_CreateOrdersTable migrated", output);
        Assert.DoesNotContain("3: TestMigration003_AddUserStatus migrating", output);
        Assert.Contains("Finished 'Migrate' to version 2 of the database.", output);
    }

    [Fact]
    public void ExecuteMigrations_WithMigrateToVersion1ThenUpgradeToLatest_AppliesRemainingMigrations()
    {
        // Arrange & Act - First migrate to version 1
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migrator1 = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetArguments(["--MigrateToVersion", "1"])
                    .SetServiceCollection(serviceCollection);
        });
        migrator1.ExecuteMigrations();

        // Act - Now migrate to latest
        serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migrator2 = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetServiceCollection(serviceCollection);
        });
        migrator2.ExecuteMigrations();

        // Assert
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // All tables should exist
        var usersTableExists = TableExists(connection, "Users");
        var ordersTableExists = TableExists(connection, "Orders");
        Assert.True(usersTableExists, "Users table should exist");
        Assert.True(ordersTableExists, "Orders table should exist");

        // All 3 migrations should be applied
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(4, migrationCount);

        // Status column should exist
        var statusColumnExists = ColumnExists(connection, "Users", "Status");
        Assert.True(statusColumnExists, "Status column should exist after full migration");
    }

    #endregion

    #region Rollback Tests

    [Fact]
    public void ExecuteMigrations_WithRollbackToVersion_RollsBackToSpecifiedVersion()
    {
        // Arrange - First run all migrations
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorUp = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetServiceCollection(serviceCollection);
        });
        migratorUp.ExecuteMigrations();

        // Act - Rollback to version 1
        _consoleOutput.GetStringBuilder().Clear();
        serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorDown = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetArguments(["--RollbackToVersion", "1"])
                    .SetServiceCollection(serviceCollection);
        });
        migratorDown.ExecuteMigrations();

        // Assert
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Users table should still exist (migration 1)
        var usersTableExists = TableExists(connection, "Users");
        Assert.True(usersTableExists, "Users table should still exist after rollback to version 1");

        // Orders table should NOT exist (migration 2 rolled back)
        var ordersTableExists = TableExists(connection, "Orders");
        Assert.False(ordersTableExists, "Orders table should not exist after rollback to version 1");

        // Only 1 migration should be recorded
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(1, migrationCount);

        // Status column should NOT exist
        var statusColumnExists = ColumnExists(connection, "Users", "Status");
        Assert.False(statusColumnExists, "Status column should not exist after rollback");

        // Verify stdout output
        var output = _consoleOutput.ToString();
        Assert.Contains("- Rollback To Version: 1", output);
        Assert.Contains("Start 'Rollback' to version 1 of the database.", output);
        Assert.Contains("3: TestMigration003_AddUserStatus reverting", output);
        Assert.Contains("DeleteColumn Users Status", output);
        Assert.Contains("2: TestMigration002_CreateOrdersTable reverting", output);
        Assert.Contains("DeleteTable Orders", output);
        Assert.Contains("Finished 'Rollback' to version 1 of the database.", output);
    }

    [Fact]
    public void ExecuteMigrations_WithRollbackToVersion0_RemovesAllMigrations()
    {
        // Arrange - First run all migrations
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorUp = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetServiceCollection(serviceCollection);
        });
        migratorUp.ExecuteMigrations();

        // Act - Rollback to version 0 (remove all)
        _consoleOutput.GetStringBuilder().Clear();
        serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorDown = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                    .SetConnectionString(_connectionString)
                    .SetArguments(["--RollbackToVersion", "0"])
                    .SetServiceCollection(serviceCollection);
        });
        migratorDown.ExecuteMigrations();

        // Assert
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // No tables should exist except VersionInfo
        var usersTableExists = TableExists(connection, "Users");
        var ordersTableExists = TableExists(connection, "Orders");
        Assert.False(usersTableExists, "Users table should not exist after complete rollback");
        Assert.False(ordersTableExists, "Orders table should not exist after complete rollback");

        // No migrations should be recorded
        var migrationCount = GetAppliedMigrationCount(connection);
        Assert.Equal(0, migrationCount);

        // Verify stdout output
        var output = _consoleOutput.ToString();
        Assert.Contains("- Rollback To Version: 0", output);
        Assert.Contains("Start 'Rollback' to version 0 of the database.", output);
        Assert.Contains("3: TestMigration003_AddUserStatus reverting", output);
        Assert.Contains("2: TestMigration002_CreateOrdersTable reverting", output);
        Assert.Contains("1: TestMigration001_CreateUsersTable reverting", output);
        Assert.Contains("DeleteTable Users", output);
        Assert.Contains("Finished 'Rollback' to version 0 of the database.", output);
    }

    #endregion

    #region Refresh Tests

    [Fact]
    public void ExecuteMigrations_WithRefreshEnabled_DropsAndRecreatesAllTables()
    {
        // Arrange - First run migrations normally
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorInitial = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetServiceCollection(serviceCollection);
        });
        migratorInitial.ExecuteMigrations();

        // Insert test data
        using (var connectionForInsert = new SqlConnection(_connectionString))
        {
            connectionForInsert.Open();
            using var commandForInsert = new SqlCommand(
                "INSERT INTO Users (Username, Email, Status) VALUES ('testuser', 'test@example.com', 'Active')",
                connectionForInsert);
            commandForInsert.ExecuteNonQuery();
        }

        // Act - Run with Refresh enabled
        _consoleOutput.GetStringBuilder().Clear();
        serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migratorRefresh = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetArguments(["--Refresh", "true"])
                   .SetServiceCollection(serviceCollection);
        });
        migratorRefresh.ExecuteMigrations();

        // Assert
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Tables should exist
        var usersTableExists = TableExists(connection, "Users");
        Assert.True(usersTableExists, "Users table should exist after refresh");

        // But data should be gone
        using var command = new SqlCommand("SELECT COUNT(*) FROM Users", connection);
        var count = (int)command.ExecuteScalar()!;
        Assert.Equal(0, count);

        // Verify stdout output
        var output = _consoleOutput.ToString();
        Assert.Contains("- Refresh: True", output);
        Assert.Contains("Start 'Refreshing' the database. !Note: All data will be lost!", output);
        Assert.Contains("3: TestMigration003_AddUserStatus reverting", output);
        Assert.Contains("1: TestMigration001_CreateUsersTable reverting", output);
        Assert.Contains("Finished 'Refreshing' the database.", output);
        Assert.Contains("Start 'Migrating' the database.", output);
        Assert.Contains("Finished 'Migrating' the database.", output);
    }

    #endregion

    #region Timeout Configuration Tests

    [Fact]
    public void ExecuteMigrations_WithCustomTimeout_UsesSpecifiedTimeout()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var customTimeout = TimeSpan.FromSeconds(120);
        var migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetTimeout(customTimeout)
                   .SetServiceCollection(serviceCollection);
        });

        // Act
        migrator.ExecuteMigrations();

        // Assert - Migrations should complete successfully with custom timeout
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var usersTableExists = TableExists(connection, "Users");
        Assert.True(usersTableExists, "Migrations should complete with custom timeout");
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public void ExecuteMigrations_AfterCompletion_AllowsDataInsertionAndRetrieval()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetServiceCollection(serviceCollection);
        });
        migrator.ExecuteMigrations();

        // Act - Insert data
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using (var command = new SqlCommand(
            @"INSERT INTO Users (Username, Email, Status) 
              VALUES ('john.doe', 'john@example.com', 'Active')",
            connection))
        {
            command.ExecuteNonQuery();
        }

        int userId;
        using (var command = new SqlCommand("SELECT SCOPE_IDENTITY()", connection))
        {
            userId = Convert.ToInt32(command.ExecuteScalar());
        }

        using (var command = new SqlCommand(
            @"INSERT INTO Orders (UserId, OrderNumber, TotalAmount, OrderDate) 
              VALUES (@UserId, 'ORD-001', 99.99, GETDATE())",
            connection))
        {
            command.Parameters.AddWithValue("@UserId", userId);
            command.ExecuteNonQuery();
        }

        // Assert - Verify data
        using (var command = new SqlCommand(
            @"SELECT u.Username, o.OrderNumber, o.TotalAmount 
              FROM Users u 
              INNER JOIN Orders o ON u.Id = o.UserId",
            connection))
        {
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read(), "Should be able to read joined data");
            Assert.Equal("john.doe", reader.GetString(0));
            Assert.Equal("ORD-001", reader.GetString(1));
            Assert.Equal(99.99m, reader.GetDecimal(2));
        }
    }

    [Fact]
    public void ExecuteMigrations_ForeignKeyConstraint_EnforcesReferentialIntegrity()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ICustomMigrationService, CustomMigrationService>();

        var migrator = Migrator.Create(options =>
        {
            options.SetAssemblyWithMigrations<MigratorTests>()
                   .SetConnectionString(_connectionString)
                   .SetServiceCollection(serviceCollection);
        });
        migrator.ExecuteMigrations();

        // Act & Assert - Try to insert order with non-existent user
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(
            @"INSERT INTO Orders (UserId, OrderNumber, TotalAmount, OrderDate) 
              VALUES (999, 'ORD-001', 99.99, GETDATE())",
            connection);

        var exception = Assert.Throws<SqlException>(() => command.ExecuteNonQuery());
        Assert.Contains("FK_Orders_Users", exception.Message);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if a table exists in the database
    /// </summary>
    private bool TableExists(SqlConnection connection, string tableName)
    {
        using var command = new SqlCommand(
            @"SELECT COUNT(*) 
              FROM INFORMATION_SCHEMA.TABLES 
              WHERE TABLE_NAME = @TableName",
            connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        var count = (int)command.ExecuteScalar()!;
        return count > 0;
    }

    /// <summary>
    /// Checks if a column exists in a table
    /// </summary>
    private bool ColumnExists(SqlConnection connection, string tableName, string columnName)
    {
        using var command = new SqlCommand(
            @"SELECT COUNT(*) 
              FROM INFORMATION_SCHEMA.COLUMNS 
              WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName",
            connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        command.Parameters.AddWithValue("@ColumnName", columnName);
        var count = (int)command.ExecuteScalar()!;
        return count > 0;
    }

    /// <summary>
    /// Gets the count of applied migrations from the VersionInfo table
    /// </summary>
    private int GetAppliedMigrationCount(SqlConnection connection)
    {
        using var command = new SqlCommand("SELECT COUNT(*) FROM Migrations.VersionInfo", connection);
        return (int)command.ExecuteScalar()!;
    }

    #endregion
}
