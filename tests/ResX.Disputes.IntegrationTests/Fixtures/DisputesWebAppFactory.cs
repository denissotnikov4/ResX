using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ResX.IntegrationTests.Common.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Disputes.IntegrationTests.Fixtures;

public sealed class DisputesWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();
    private bool _respawnerReady;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DisputesDb"] = _postgres.ConnectionString,
                ["Jwt:SecretKey"] = JwtTokenHelper.TestSecretKey,
                ["Jwt:Issuer"] = JwtTokenHelper.TestIssuer,
                ["Jwt:Audience"] = JwtTokenHelper.TestAudience,
                ["Jwt:ExpiryMinutes"] = "60",
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DisputesDb", _postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtTokenHelper.TestSecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTokenHelper.TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTokenHelper.TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");

        _ = CreateClient();
        await _postgres.InitializeRespawnerAsync(["disputes"]);
        _respawnerReady = true;
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawnerReady)
            await _postgres.ResetAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DisputesDb", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
