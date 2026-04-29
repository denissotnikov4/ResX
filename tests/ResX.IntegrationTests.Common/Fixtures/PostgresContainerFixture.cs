using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace ResX.IntegrationTests.Common.Fixtures;

/// <summary>
/// Manages a single PostgreSQL Testcontainer shared across all test classes in a collection.
/// Respawn resets the database state between each test class's test run.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("resx_test")
        .WithUsername("resx_test")
        .WithPassword("resx_test_pass")
        .WithCleanUp(true)
        .Build();

    private Respawner? _respawner;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    /// <summary>
    /// Must be called after the first WebApplicationFactory startup (which runs migrations).
    /// Subsequent calls to ResetAsync will delete all test data while keeping the schema.
    /// </summary>
    public async Task InitializeRespawnerAsync(string[] schemasToInclude, string[]? additionalTablesToIgnore = null)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var tablesToIgnore = new List<Respawn.Graph.Table>
        {
            // Never delete FluentMigrator version tracking
            new("VersionInfo")
        };

        if (additionalTablesToIgnore is not null)
        {
            tablesToIgnore.AddRange(additionalTablesToIgnore.Select(t => new Respawn.Graph.Table(t)));
        }

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = schemasToInclude,
            TablesToIgnore = tablesToIgnore.ToArray()
        });
    }

    public async Task ResetAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException(
                "Call InitializeRespawnerAsync first (after migrations have run).");

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
