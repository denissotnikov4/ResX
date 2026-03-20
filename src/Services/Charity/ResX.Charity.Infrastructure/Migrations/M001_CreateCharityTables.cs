using FluentMigrator;

namespace ResX.Charity.Infrastructure.Migrations;

[Migration(1, "Create charity tables")]
public class M001_CreateCharityTables : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS charity;");

        Create.Table("organizations").InSchema("charity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(2000).NotNullable()
            .WithColumn("verification_status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("legal_document_url").AsString(1000).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.Table("charity_requests").InSchema("charity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("organization_id").AsGuid().NotNullable()
                .ForeignKey("fk_charity_requests_organizations", "charity", "organizations", "id")
            .WithColumn("title").AsString(200).NotNullable()
            .WithColumn("description").AsString(5000).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Active")
            .WithColumn("deadline_date").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().Nullable();

        Create.Table("requested_items").InSchema("charity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("charity_request_id").AsGuid().NotNullable()
                .ForeignKey("fk_requested_items_charity_requests", "charity", "charity_requests", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("category_id").AsGuid().NotNullable()
            .WithColumn("category_name").AsString(100).NotNullable()
            .WithColumn("quantity_needed").AsInt32().NotNullable()
            .WithColumn("quantity_received").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("condition").AsString(50).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("requested_items").InSchema("charity");
        Delete.Table("charity_requests").InSchema("charity");
        Delete.Table("organizations").InSchema("charity");
    }
}
