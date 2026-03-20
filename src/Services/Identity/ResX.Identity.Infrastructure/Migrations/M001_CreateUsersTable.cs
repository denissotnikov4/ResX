using FluentMigrator;

namespace ResX.Identity.Infrastructure.Migrations;

[Migration(1, "Create users table")]
public class M001_CreateUsersTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS identity;");

        Create.Table("users").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("email").AsString(256).NotNullable().Unique()
            .WithColumn("phone").AsString(20).Nullable()
            .WithColumn("password_hash").AsString(512).NotNullable()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("role").AsString(50).NotNullable().WithDefaultValue("Donor")
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_users_email").OnTable("users").InSchema("identity")
            .OnColumn("email").Ascending().WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("users").InSchema("identity");
        Execute.Sql("DROP SCHEMA IF EXISTS identity;");
    }
}
