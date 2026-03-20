using Microsoft.Extensions.Configuration;
using Npgsql;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Repositories;

namespace ResX.Analytics.Infrastructure.Persistence;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly string _usersDb;
    private readonly string _listingsDb;
    private readonly string _transactionsDb;

    public AnalyticsRepository(IConfiguration configuration)
    {
        _usersDb = configuration.GetConnectionString("UsersDb")!;
        _listingsDb = configuration.GetConnectionString("ListingsDb")!;
        _transactionsDb = configuration.GetConnectionString("TransactionsDb")!;
    }

    public async Task<EcoPlatformStatsDto> GetEcoStatsAsync(CancellationToken cancellationToken = default)
    {
        // Query across databases using raw SQL for analytics
        long totalItemsTransferred = 0;
        decimal totalCo2Saved = 0, totalWasteSaved = 0;
        int activeListings = 0, registeredUsers = 0;

        await using var usersConn = new NpgsqlConnection(_usersDb);
        await usersConn.OpenAsync(cancellationToken);

        await using var userCmd = new NpgsqlCommand(
            "SELECT COUNT(*), COALESCE(SUM(items_gifted), 0), COALESCE(SUM(co2_saved_kg), 0), COALESCE(SUM(waste_saved_kg), 0) FROM users.user_profiles",
            usersConn);
        await using var userReader = await userCmd.ExecuteReaderAsync(cancellationToken);
        if (await userReader.ReadAsync(cancellationToken))
        {
            registeredUsers = (int)userReader.GetInt64(0);
            totalItemsTransferred = userReader.GetInt64(1);
            totalCo2Saved = userReader.GetDecimal(2);
            totalWasteSaved = userReader.GetDecimal(3);
        }
        await userReader.CloseAsync();
        await usersConn.CloseAsync();

        await using var listingsConn = new NpgsqlConnection(_listingsDb);
        await listingsConn.OpenAsync(cancellationToken);
        await using var listingsCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM listings.listings WHERE status = 'Active'",
            listingsConn);
        activeListings = (int)(long)(await listingsCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
        await listingsConn.CloseAsync();

        return new EcoPlatformStatsDto(
            totalItemsTransferred, totalCo2Saved, totalWasteSaved,
            activeListings, registeredUsers, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<CategoryStatsDto>> GetCategoryStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new List<CategoryStatsDto>();

        await using var conn = new NpgsqlConnection(_listingsDb);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            @"SELECT category_id, category_name, COUNT(*) as count
              FROM listings.listings
              WHERE status IN ('Active', 'Completed')
              GROUP BY category_id, category_name
              ORDER BY count DESC
              LIMIT 20",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(new CategoryStatsDto(
                reader.GetGuid(0), reader.GetString(1),
                (int)reader.GetInt64(2), 0));
        }

        return stats.AsReadOnly();
    }

    public async Task<IReadOnlyList<CityStatsDto>> GetCityStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new List<CityStatsDto>();

        await using var conn = new NpgsqlConnection(_listingsDb);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            @"SELECT city, COUNT(*) as listings_count
              FROM listings.listings
              GROUP BY city
              ORDER BY listings_count DESC
              LIMIT 20",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(new CityStatsDto(reader.GetString(0), (int)reader.GetInt64(1), 0, 0));
        }

        return stats.AsReadOnly();
    }
}
