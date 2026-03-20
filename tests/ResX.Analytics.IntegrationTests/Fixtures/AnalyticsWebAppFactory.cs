using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Queries;
using ResX.Analytics.Application.Repositories;
using Xunit;

namespace ResX.Analytics.IntegrationTests.Fixtures;

/// <summary>
/// Analytics service does not own a database — it runs cross-DB raw SQL queries
/// via IAnalyticsRepository. We mock the repository so tests are fully in-process
/// without requiring any running databases.
/// </summary>
public sealed class AnalyticsWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IAnalyticsRepository RepositoryMock { get; } = Substitute.For<IAnalyticsRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAnalyticsRepository>();
            services.AddScoped(_ => RepositoryMock);
        });

        // Seed default responses so the mock never throws by default
        RepositoryMock.GetEcoStatsAsync(CancellationToken.None)
            .ReturnsForAnyArgs(new EcoPlatformStatsDto(1234, 56.7m, 89.0m, 42, 300, DateTime.UtcNow));

        RepositoryMock.GetCategoryStatsAsync(CancellationToken.None)
            .ReturnsForAnyArgs(new List<CategoryStatsDto>
            {
                new(Guid.NewGuid(), "Мебель", 10, 3),
                new(Guid.NewGuid(), "Одежда", 7, 2)
            });

        RepositoryMock.GetCityStatsAsync(CancellationToken.None)
            .ReturnsForAnyArgs(new List<CityStatsDto>
            {
                new("Москва", 50, 120, 33.4m),
                new("Казань", 20, 45, 12.1m)
            });
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
