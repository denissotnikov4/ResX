using FluentMigrator;

namespace ResX.Users.Infrastructure.Migrations;

[Migration(2, "Create reviews table")]
public class M002_CreateReviewsTable : Migration
{
    public override void Up()
    {
        Create.Table("reviews").InSchema("users")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_profile_id").AsGuid().NotNullable()
                .ForeignKey("fk_reviews_user_profiles", "users", "user_profiles", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("reviewer_id").AsGuid().NotNullable()
            .WithColumn("reviewer_name").AsString(200).NotNullable()
            .WithColumn("rating").AsInt32().NotNullable()
            .WithColumn("comment").AsString(2000).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.Index("ix_reviews_user_profile_id").OnTable("reviews").InSchema("users")
            .OnColumn("user_profile_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("reviews").InSchema("users");
    }
}
