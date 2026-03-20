using FluentMigrator;

namespace ResX.Identity.Infrastructure.Migrations;

[Migration(2, "Create refresh tokens table")]
public class M002_CreateRefreshTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("refresh_tokens").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
                .ForeignKey("fk_refresh_tokens_users", "identity", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("token").AsString(512).NotNullable().Unique()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
            .WithColumn("is_revoked").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.Index("ix_refresh_tokens_token").OnTable("refresh_tokens").InSchema("identity")
            .OnColumn("token").Ascending().WithOptions().Unique();

        Create.Index("ix_refresh_tokens_user_id").OnTable("refresh_tokens").InSchema("identity")
            .OnColumn("user_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("refresh_tokens").InSchema("identity");
    }
}
