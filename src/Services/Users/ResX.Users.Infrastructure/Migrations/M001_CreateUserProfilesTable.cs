using FluentMigrator;

namespace ResX.Users.Infrastructure.Migrations;

[Migration(1, "Create user profiles table")]
public class M001_CreateUserProfilesTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS users;");

        Create.Table("user_profiles").InSchema("users")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("avatar_url").AsString(1000).Nullable()
            .WithColumn("bio").AsString(1000).Nullable()
            .WithColumn("city").AsString(100).Nullable()
            .WithColumn("rating").AsDecimal(3, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("review_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("items_gifted").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("items_received").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("co2_saved_kg").AsDecimal(10, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("waste_saved_kg").AsDecimal(10, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().Nullable();
    }

    public override void Down()
    {
        Delete.Table("user_profiles").InSchema("users");
    }
}
