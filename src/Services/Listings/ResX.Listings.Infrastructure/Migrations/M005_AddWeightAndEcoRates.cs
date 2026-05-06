using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(5, "Add weight to listings + eco rates to categories + computed eco values to listings")]
public class M005_AddWeightAndEcoRates : Migration
{
    public override void Up()
    {
        // Eco rates per 100 grams of product (in grams of CO2 / waste).
        // Default 0 — categories that already exist won't have impact until admin sets the rate.
        Alter.Table("categories").InSchema("listings")
            .AddColumn("co2_saved_per_100g_g").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("waste_saved_per_100g_g").AsInt32().NotNullable().WithDefaultValue(0);

        // Listing weight + computed eco values cached on the listing
        // (so changing a category rate doesn't retroactively change historical listings).
        Alter.Table("listings").InSchema("listings")
            .AddColumn("weight_grams").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("co2_saved_g").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("waste_saved_g").AsInt32().NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("waste_saved_g").FromTable("listings").InSchema("listings");
        Delete.Column("co2_saved_g").FromTable("listings").InSchema("listings");
        Delete.Column("weight_grams").FromTable("listings").InSchema("listings");

        Delete.Column("waste_saved_per_100g_g").FromTable("categories").InSchema("listings");
        Delete.Column("co2_saved_per_100g_g").FromTable("categories").InSchema("listings");
    }
}
