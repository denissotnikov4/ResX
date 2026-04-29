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
public sealed class CreateListingTests : IAsyncLifetime
{
    private readonly ListingsWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateListingTests(ListingsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateListing_WithValidData_Returns201WithId()
    {
        // Arrange
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto();

        // Act
        var response = await _client.PostJsonAsync("/api/listings", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.ReadAsAsync<IdResponse>();
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateListing_Returns201_LocationHeaderPointsToNewResource()
    {
        _client.WithBearerToken(GenerateToken());

        var response = await _client.PostJsonAsync("/api/listings", ValidCreateListingDto());

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/listings/");
    }

    [Fact]
    public async Task CreateListing_WithTags_Returns201()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with
        {
            Tags = ["мебель", "диван", "бесплатно"]
        };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateListing_CharityType_Returns201()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with { TransferType = TransferType.Charity };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // -------------------------------------------------------------------------
    // Authentication required
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateListing_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/listings", ValidCreateListingDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateListing_WithExpiredToken_Returns401()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateExpiredToken(_userId, "user@test.com"));
        var response = await _client.PostJsonAsync("/api/listings", ValidCreateListingDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Validation errors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateListing_EmptyTitle_Returns422()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with { Title = "" };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateListing_TitleTooLong_Returns422()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with { Title = new string('A', 201) };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateListing_EmptyDescription_Returns422()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with { Description = string.Empty };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateListing_EmptyCity_Returns422()
    {
        _client.WithBearerToken(GenerateToken());
        var dto = ValidCreateListingDto() with { City = string.Empty };

        var response = await _client.PostJsonAsync("/api/listings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GenerateToken() =>
        JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com");

    private static readonly Guid SeededFurnitureCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111103");

    private static CreateListingDto ValidCreateListingDto() => new(
        Title: FakerExtensions.RandomTitle(),
        Description: FakerExtensions.RandomDescription(),
        CategoryId: SeededFurnitureCategoryId,
        Condition: ItemCondition.Good,
        TransferType: TransferType.Gift,
        TransferMethod: TransferMethod.InPerson,
        City: FakerExtensions.RandomCity(),
        District: null,
        Latitude: null,
        Longitude: null,
        Tags: null);

    private sealed record IdResponse(Guid Id);
}
