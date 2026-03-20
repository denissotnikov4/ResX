using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ResX.Caching.Redis.Abstractions;
using ResX.EventBus.RabbitMQ;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.IntegrationTests.Common.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using StackExchange.Redis;
using Xunit;

namespace ResX.Identity.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory for the Identity service.
/// - PostgreSQL: real Testcontainers instance (FluentMigrator runs migrations on startup).
/// - Redis: NSubstitute mock — safe because IConnectionMultiplexer is now registered via a
///   lazy factory in production DI, so ConfigureTestServices replaces it before first use.
/// - RabbitMQ: NSubstitute mock (RabbitMQConnection only connects on first publish).
/// </summary>
public sealed class IdentityWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();
    private bool _respawnerReady;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDb"] = _postgres.ConnectionString,
                ["Jwt:SecretKey"] = JwtTokenHelper.TestSecretKey,
                ["Jwt:Issuer"] = JwtTokenHelper.TestIssuer,
                ["Jwt:Audience"] = JwtTokenHelper.TestAudience,
                ["Jwt:ExpiryMinutes"] = "60",
                ["RabbitMQ:HostName"] = "localhost",
                ["RabbitMQ:UserName"] = "guest",
                ["RabbitMQ:Password"] = "guest",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // IConnectionMultiplexer is now lazy in production code, so we can safely replace it here.
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<ICacheService>();
            services.AddSingleton(Substitute.For<IConnectionMultiplexer>());
            services.AddSingleton(Substitute.For<ICacheService>());

            services.RemoveAll<RabbitMQConnection>();
            services.RemoveAll<IEventBus>();
            services.AddSingleton(Substitute.For<IEventBus>());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        // Environment variables are read by WebApplication.CreateBuilder() before
        // service registration runs, so FluentMigrator's WithGlobalConnectionString()
        // will capture the test container value instead of appsettings.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", _postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtTokenHelper.TestSecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTokenHelper.TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTokenHelper.TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");

        _ = CreateClient();
        await _postgres.InitializeRespawnerAsync(["identity"]);
        _respawnerReady = true;
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawnerReady)
            await _postgres.ResetAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
