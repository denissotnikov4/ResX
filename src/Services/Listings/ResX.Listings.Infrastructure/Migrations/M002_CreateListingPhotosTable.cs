using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(2, "Create listing photos table")]
public class M002_CreateListingPhotosTable : Migration
{
    public override void Up()
    {
        Create.Table("listing_photos").InSchema("listings")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("listing_id").AsGuid().NotNullable()
                .ForeignKey("fk_listing_photos_listings", "listings", "listings", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("url").AsString(1000).NotNullable()
            .WithColumn("display_order").AsInt32().NotNullable().WithDefaultValue(0);

        Create.Index("ix_listing_photos_listing_id").OnTable("listing_photos").InSchema("listings")
            .OnColumn("listing_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("listing_photos").InSchema("listings");
    }
}
