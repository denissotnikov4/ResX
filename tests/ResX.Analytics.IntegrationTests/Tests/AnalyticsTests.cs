using System.Net;
using FluentAssertions;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.IntegrationTests.Collections;
using ResX.Analytics.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Analytics.IntegrationTests.Tests;

[Collection(AnalyticsCollection.Name)]
public sealed class AnalyticsTests
{
    private readonly HttpClient _client;

    public AnalyticsTests(AnalyticsWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/analytics/eco-stats
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEcoStats_Returns200WithExpectedShape()
    {
        var response = await _client.GetAsync("/api/analytics/eco-stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.ReadAsAsync<EcoPlatformStatsDto>();
        stats.TotalItemsTransferred.Should().Be(1234);
        stats.RegisteredUsers.Should().Be(300);
        stats.ActiveListings.Should().Be(42);
        stats.TotalCo2SavedKg.Should().BeGreaterThan(0);
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    // -------------------------------------------------------------------------
    // GET /api/analytics/categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCategoryStats_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/analytics/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.ReadAsAsync<List<CategoryStatsDto>>();
        list.Should().HaveCount(2);
        list[0].CategoryName.Should().Be("Мебель");
    }

    // -------------------------------------------------------------------------
    // GET /api/analytics/cities
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCityStats_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/analytics/cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.ReadAsAsync<List<CityStatsDto>>();
        list.Should().HaveCount(2);
        list[0].City.Should().Be("Москва");
        list[0].ListingsCount.Should().Be(50);
    }

    [Fact]
    public async Task GetEcoStats_DoesNotRequireAuth()
    {
        // Analytics endpoints are public — no auth header needed
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/analytics/eco-stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
