using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Users.IntegrationTests.Collections;
using ResX.Users.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Users.IntegrationTests.Tests;

[Collection(UsersCollection.Name)]
public sealed class ReviewTests : IAsyncLifetime
{
    private readonly UsersWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _targetUserId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();

    public ReviewTests(UsersWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.SeedProfileAsync(_targetUserId, "Мария", "Иванова");
        await _factory.SeedProfileAsync(_reviewerId, "Петр", "Сидоров");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/users/{id}/reviews
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetReviews_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync($"/api/users/{_targetUserId}/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // POST /api/users/{id}/reviews
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddReview_ValidData_Returns200WithReviewId()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_reviewerId, "reviewer@test.com"));

        var response = await _client.PostJsonAsync($"/api/users/{_targetUserId}/reviews", new
        {
            ReviewerName = "Петр",
            Rating = 5,
            Comment = "Отличный донор, всё чисто и вовремя!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<ReviewIdResponse>();
        body.ReviewId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddReview_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync($"/api/users/{_targetUserId}/reviews", new
        {
            ReviewerName = "Анон", Rating = 3, Comment = "Нормально"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddReview_ToSelf_Returns400()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_targetUserId, "self@test.com"));

        var response = await _client.PostJsonAsync($"/api/users/{_targetUserId}/reviews", new
        {
            ReviewerName = "Я", Rating = 5, Comment = "Сам себя хвалю"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReviews_AfterAddingReview_ContainsIt()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_reviewerId, "reviewer@test.com"));

        await _client.PostJsonAsync($"/api/users/{_targetUserId}/reviews", new
        {
            ReviewerName = "Петр", Rating = 4, Comment = "Хорошо"
        });

        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/users/{_targetUserId}/reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ReviewIdResponse(Guid ReviewId);
}
