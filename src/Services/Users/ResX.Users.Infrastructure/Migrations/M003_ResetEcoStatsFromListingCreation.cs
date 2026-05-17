using FluentMigrator;

namespace ResX.Users.Infrastructure.Migrations;

/// <summary>
/// One-time cleanup: an earlier version incremented EcoStats whenever a listing was
/// created, which inflated lifetime counters. We've moved the trigger to
/// TransactionCompleted; this migration zeroes accrued values so the new flow can
/// build them up cleanly. Idempotent.
/// </summary>
[Migration(3, "Zero out eco stats inflated by ListingCreated-based accrual")]
public class M003_ResetEcoStatsFromListingCreation : Migration
{
    public override void Up()
    {
        Execute.Sql(
            "UPDATE users.user_profiles " +
            "SET items_gifted = 0, items_received = 0, co2_saved_kg = 0, waste_saved_kg = 0;");
    }

    public override void Down()
    {
        // Not reversible — original values are lost. Down is a no-op.
    }
}
