using FluentMigrator;

namespace ResX.Listings.Infrastructure.Migrations;

[Migration(6, "Seed reasonable CO2 and waste rates for the default categories")]
public class M006_SeedDefaultCategoryEcoRates : Migration
{
    // Rates are grams of impact per 100 grams of product transferred.
    // CO2 figures are rough order-of-magnitude estimates of avoided
    // production emissions; waste is assumed = item weight (full diversion).
    // Admin can tune any value through PUT /api/categories/{id}.
    public override void Up()
    {
        Update("11111111-1111-1111-1111-111111111101", co2: 300, waste: 100); // Clothing
        Update("11111111-1111-1111-1111-111111111102", co2: 600, waste: 100); // Electronics
        Update("11111111-1111-1111-1111-111111111103", co2: 100, waste: 100); // Furniture
        Update("11111111-1111-1111-1111-111111111104", co2: 30,  waste: 100); // Books
        Update("11111111-1111-1111-1111-111111111105", co2: 50,  waste: 100); // Toys & Games
    }

    public override void Down()
    {
        // Revert to zeros for the same seeded ids.
        Update("11111111-1111-1111-1111-111111111101", co2: 0, waste: 0);
        Update("11111111-1111-1111-1111-111111111102", co2: 0, waste: 0);
        Update("11111111-1111-1111-1111-111111111103", co2: 0, waste: 0);
        Update("11111111-1111-1111-1111-111111111104", co2: 0, waste: 0);
        Update("11111111-1111-1111-1111-111111111105", co2: 0, waste: 0);
    }

    private void Update(string id, int co2, int waste)
    {
        Execute.Sql(
            $"UPDATE listings.categories " +
            $"SET co2_saved_per_100g_g = {co2}, waste_saved_per_100g_g = {waste} " +
            $"WHERE id = '{id}';");
    }
}
