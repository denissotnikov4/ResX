using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(1, "Create listings table")]
public class M001_CreateListingsTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS listings;");

        Create.Table("listings").InSchema("listings")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("title").AsString(200).NotNullable()
            .WithColumn("description").AsString(5000).NotNullable()
            .WithColumn("category_id").AsGuid().NotNullable()
            .WithColumn("category_name").AsString(100).NotNullable()
            .WithColumn("parent_category_id").AsGuid().Nullable()
            .WithColumn("condition").AsString(50).NotNullable()
            .WithColumn("transfer_type").AsString(50).NotNullable()
            .WithColumn("transfer_method").AsString(50).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Active")
            .WithColumn("city").AsString(100).NotNullable()
            .WithColumn("district").AsString(100).Nullable()
            .WithColumn("latitude").AsDouble().Nullable()
            .WithColumn("longitude").AsDouble().Nullable()
            .WithColumn("donor_id").AsGuid().NotNullable()
            .WithColumn("tags").AsString(1000).Nullable()
            .WithColumn("view_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_listings_donor_id").OnTable("listings").InSchema("listings")
            .OnColumn("donor_id").Ascending();
        Create.Index("ix_listings_status").OnTable("listings").InSchema("listings")
            .OnColumn("status").Ascending();
        Create.Index("ix_listings_created_at").OnTable("listings").InSchema("listings")
            .OnColumn("created_at").Descending();
        Create.Index("ix_listings_city").OnTable("listings").InSchema("listings")
            .OnColumn("city").Ascending();
    }

    public override void Down()
    {
        Delete.Table("listings").InSchema("listings");
    }
}
