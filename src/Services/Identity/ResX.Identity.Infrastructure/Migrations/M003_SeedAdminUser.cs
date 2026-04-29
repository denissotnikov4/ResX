using FluentMigrator;

namespace ResX.Identity.Infrastructure.Migrations;

[Migration(3, "Seed default admin user")]
public class M003_SeedAdminUser : Migration
{
    private static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const string AdminEmail = "admin@resx.local";

    // BCrypt hash of "Admin123!" (work factor 12). Change password via /api/auth/change-password before going to prod.
    private const string AdminPasswordHash = "$2a$12$tqkxVnt71xhCKe9os9ZqxOIiUseINq8UAO2h/ueaWbruZTRZrXAMC";

    public override void Up()
    {
        Insert.IntoTable("users").InSchema("identity").Row(new
        {
            id = AdminId,
            email = AdminEmail,
            phone = (object)DBNull.Value,
            password_hash = AdminPasswordHash,
            first_name = "Admin",
            last_name = "ResX",
            role = "Admin",
            is_active = true
        });
    }

    public override void Down()
    {
        Delete.FromTable("users").InSchema("identity")
            .Row(new { id = AdminId });
    }
}
