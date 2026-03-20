using Testcontainers.Redis;
using Xunit;

namespace ResX.IntegrationTests.Common.Fixtures;

/// <summary>
/// Manages a single Redis Testcontainer.
/// Use alongside <see cref="PostgresContainerFixture"/> inside a WebApplicationFactory.
/// </summary>
public sealed class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// StackExchange.Redis connection string, e.g. "localhost:32768".
    /// Pass this as ConnectionStrings:Redis (or Redis:ConnectionString) in test app config.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
