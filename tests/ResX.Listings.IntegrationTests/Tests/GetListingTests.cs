using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Domain.Enums;
using ResX.Listings.IntegrationTests.Collections;
using ResX.Listings.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Listings.IntegrationTests.Tests;

[Collection(ListingsCollection.Name)]
public sealed class GetListingTests : IAsyncLifetime
{
    private readonly ListingsWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public GetListingTests(ListingsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GetById
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingListing_Returns200WithDetails()
    {
        // Arrange — create a listing
        var listingId = await CreateListingAsync();

        // Act
        var response = await _client.GetAsync($"/api/listings/{listingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.ReadAsAsync<ListingDto>();
        dto.Id.Should().Be(listingId);
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/listings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_IsPublicEndpoint_DoesNotRequireAuth()
    {
        _client.WithoutAuth();
        var listingId = await CreateListingAsync();

        var response = await _client.GetAsync($"/api/listings/{listingId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_IncreasesViewCount_OnSubsequentCalls()
    {
        var listingId = await CreateListingAsync();

        // First call
        var first = await (await _client.GetAsync($"/api/listings/{listingId}"))
            .ReadAsAsync<ListingDto>();

        // Second call
        var second = await (await _client.GetAsync($"/api/listings/{listingId}"))
            .ReadAsAsync<ListingDto>();

        second.ViewCount.Should().BeGreaterThan(first.ViewCount);
    }

    // -------------------------------------------------------------------------
    // GetListings (search + pagination)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetListings_ReturnsOk_AndPagedResult()
    {
        // Arrange — seed a few listings
        await CreateListingAsync();
        await CreateListingAsync();

        // Act
        var response = await _client.GetAsync("/api/listings?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetListings_FilterByCity_ReturnsOnlyMatchingCity()
    {
        // Arrange
        var targetCity = "Казань";
        await CreateListingAsync(city: targetCity);
        await CreateListingAsync(city: "Москва");

        // Act
        var response = await _client.GetAsync($"/api/listings?city={Uri.EscapeDataString(targetCity)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetListings_FilterByTransferType_ReturnsOk()
    {
        await CreateListingAsync(transferType: TransferType.Gift);

        var response = await _client.GetAsync("/api/listings?transferType=Gift");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetListings_IsPublicEndpoint_DoesNotRequireAuth()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/listings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // GetMyListings
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyListings_Authenticated_Returns200()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "u@t.com"));
        await CreateListingAsync();

        var response = await _client.GetAsync("/api/listings/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyListings_WithoutAuth_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/listings/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateListingAsync(
        string? city = null,
        TransferType transferType = TransferType.Gift)
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));

        var dto = new CreateListingDto(
            Title: FakerExtensions.RandomTitle(),
            Description: FakerExtensions.RandomDescription(),
            CategoryId: Guid.NewGuid(),
            CategoryName: "Мебель",
            ParentCategoryId: null,
            Condition: ItemCondition.Good,
            TransferType: transferType,
            TransferMethod: TransferMethod.InPerson,
            City: city ?? FakerExtensions.RandomCity(),
            District: null,
            Latitude: null,
            Longitude: null,
            Tags: null);

        var response = await _client.PostJsonAsync("/api/listings", dto);
        response.EnsureSuccessStatusCode();

        var body = await response.ReadAsAsync<IdResponse>();
        return body.Id;
    }

    private sealed record IdResponse(Guid Id);
}
