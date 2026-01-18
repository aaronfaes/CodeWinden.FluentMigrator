using FluentMigrator;

namespace CodeWinden.FluentMigrator.Tests.TestMigrations;

/// <summary>
/// Test migration that demonstrates dependency injection of custom services
/// </summary>
[Migration(4)]
public class TestMigration004_WithCustomService : Migration
{
    private readonly ICustomMigrationService _customService;

    /// <summary>
    /// Constructor that receives custom service via dependency injection
    /// </summary>
    public TestMigration004_WithCustomService(ICustomMigrationService customService)
    {
        _customService = customService;
    }

    public override void Up()
    {
        // Log that this migration is executing using the custom service
        _customService.LogMigrationExecution(nameof(TestMigration004_WithCustomService));

        // Create a simple table to verify migration executed
        Create.Table("MigrationLog")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("MigrationName").AsString(255).NotNullable()
            .WithColumn("ExecutedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("MigrationLog");
    }
}
