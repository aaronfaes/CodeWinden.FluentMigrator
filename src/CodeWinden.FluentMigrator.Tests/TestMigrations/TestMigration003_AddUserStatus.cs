using FluentMigrator;

namespace CodeWinden.FluentMigrator.Tests.TestMigrations;

/// <summary>
/// Test migration that adds a Status column to the Users table
/// </summary>
[Migration(3, "Add Status column to Users table")]
public class TestMigration003_AddUserStatus : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active");
    }

    public override void Down()
    {
        Delete.Column("Status").FromTable("Users");
    }
}
