using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ResX.Files.Infrastructure;
using ResX.IntegrationTests.Common.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Storage.S3.Abstractions;
using Xunit;

namespace ResX.Files.IntegrationTests.Fixtures;

public sealed class FilesWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();
    private bool _respawnerReady;

    /// <summary>Exposed so tests can configure return values per-scenario.</summary>
    public IStorageService StorageMock { get; } = Substitute.For<IStorageService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FilesDb"] = _postgres.ConnectionString,
                ["Jwt:SecretKey"] = JwtTokenHelper.TestSecretKey,
                ["Jwt:Issuer"] = JwtTokenHelper.TestIssuer,
                ["Jwt:Audience"] = JwtTokenHelper.TestAudience,
                ["Jwt:ExpiryMinutes"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace the real S3 storage with a mock that returns predictable values
            services.RemoveAll<IStorageService>();
            services.AddSingleton(StorageMock);

            StorageMock
                .UploadAsync(default!, default!, default!, default)
                .ReturnsForAnyArgs("files/test-storage-key.jpg");

            StorageMock
                .GetPresignedUrlAsync(default!, default, default)
                .ReturnsForAnyArgs("https://cdn.example.com/files/test-storage-key.jpg");
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__FilesDb", _postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtTokenHelper.TestSecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTokenHelper.TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTokenHelper.TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");

        _ = CreateClient();
        await _postgres.InitializeRespawnerAsync(["files"]);
        _respawnerReady = true;
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawnerReady)
            await _postgres.ResetAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__FilesDb", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
