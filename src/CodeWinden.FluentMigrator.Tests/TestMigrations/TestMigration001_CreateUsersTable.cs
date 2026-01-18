using FluentMigrator;

namespace CodeWinden.FluentMigrator.Tests.TestMigrations;

/// <summary>
/// Test migration that creates a Users table
/// </summary>
[Migration(1, "Create Users table")]
public class TestMigration001_CreateUsersTable : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Username").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
    }
}
