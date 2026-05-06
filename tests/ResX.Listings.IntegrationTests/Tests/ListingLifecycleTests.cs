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
public sealed class ListingLifecycleTests : IAsyncLifetime
{
    private readonly ListingsWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public ListingLifecycleTests(ListingsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Update listing
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateListing_ByOwner_Returns204()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        var updated = ValidDto() with { Title = "Updated Title" };
        var response = await _client.PutJsonAsync($"/api/listings/{listingId}", updated);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateListing_ByOtherUser_Returns403OrNotFound()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_otherUserId, "other@test.com"));

        var response = await _client.PutJsonAsync($"/api/listings/{listingId}", ValidDto());

        ((int)response.StatusCode).Should().BeOneOf(403, 404);
    }

    [Fact]
    public async Task UpdateListing_WithoutAuth_Returns401()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithoutAuth();

        var response = await _client.PutJsonAsync($"/api/listings/{listingId}", ValidDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Change status
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_Draft_To_Active_ByOwner_Returns204()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        var response = await _client.PatchJsonAsync(
            $"/api/listings/{listingId}/status",
            new { Status = ListingStatus.Active });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeStatus_WithoutAuth_Returns401()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithoutAuth();

        var response = await _client.PatchJsonAsync(
            $"/api/listings/{listingId}/status",
            new { Status = ListingStatus.Active });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Delete listing
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteListing_ByOwner_Returns204()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        var response = await _client.DeleteAsync($"/api/listings/{listingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteListing_ByOwner_ThenGetById_Returns404()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        await _client.DeleteAsync($"/api/listings/{listingId}");

        _client.WithoutAuth();
        var getResponse = await _client.GetAsync($"/api/listings/{listingId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteListing_ByOtherUser_Returns403OrNotFound()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_otherUserId, "other@test.com"));

        var response = await _client.DeleteAsync($"/api/listings/{listingId}");

        ((int)response.StatusCode).Should().BeOneOf(403, 404);
    }

    [Fact]
    public async Task DeleteListing_NonExistentId_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        var response = await _client.DeleteAsync($"/api/listings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Add photo
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddPhoto_ByOwner_Returns200WithPhotoId()
    {
        var listingId = await CreateListingAsOwner();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));

        var response = await _client.PostJsonAsync(
            $"/api/listings/{listingId}/photos",
            new { Url = "https://cdn.example.com/photo.jpg", DisplayOrder = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("photoId");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateListingAsOwner()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_ownerId, "owner@test.com"));
        var response = await _client.PostJsonAsync("/api/listings", ValidDto());
        response.EnsureSuccessStatusCode();
        var body = await response.ReadAsAsync<IdResponse>();
        return body.Id;
    }

    private static CreateListingDto ValidDto() => new(
        Title: FakerExtensions.RandomTitle(),
        Description: FakerExtensions.RandomDescription(),
        CategoryId: Guid.Parse("11111111-1111-1111-1111-111111111102"),
        WeightGrams: 800,
        Condition: ItemCondition.LikeNew,
        TransferType: TransferType.Gift,
        TransferMethod: TransferMethod.Both,
        City: "Москва",
        District: "Центральный",
        Latitude: 55.7558,
        Longitude: 37.6173,
        Tags: ["телефон", "samsung"]);

    private sealed record IdResponse(Guid Id);
}
