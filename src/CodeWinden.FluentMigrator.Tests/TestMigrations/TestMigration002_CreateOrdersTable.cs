using FluentMigrator;

namespace CodeWinden.FluentMigrator.Tests.TestMigrations;

/// <summary>
/// Test migration that creates an Orders table with foreign key to Users
/// </summary>
[Migration(2, "Create Orders table")]
public class TestMigration002_CreateOrdersTable : Migration
{
    public override void Up()
    {
        Create.Table("Orders")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("OrderNumber").AsString(50).NotNullable()
            .WithColumn("TotalAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("OrderDate").AsDateTime().NotNullable();

        Create.ForeignKey("FK_Orders_Users")
            .FromTable("Orders").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_Orders_UserId")
            .OnTable("Orders")
            .OnColumn("UserId");
    }

    public override void Down()
    {
        Delete.Index("IX_Orders_UserId").OnTable("Orders");
        Delete.ForeignKey("FK_Orders_Users").OnTable("Orders");
        Delete.Table("Orders");
    }
}
