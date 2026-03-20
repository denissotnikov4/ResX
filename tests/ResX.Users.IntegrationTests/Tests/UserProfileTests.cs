using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Users.Application.DTOs;
using ResX.Users.IntegrationTests.Collections;
using ResX.Users.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Users.IntegrationTests.Tests;

[Collection(UsersCollection.Name)]
public sealed class UserProfileTests : IAsyncLifetime
{
    private readonly UsersWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public UserProfileTests(UsersWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.SeedProfileAsync(_userId, FakerExtensions.RandomFirstName(), FakerExtensions.RandomLastName(), FakerExtensions.RandomCity());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/users/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetProfile_ExistingUser_Returns200WithProfile()
    {
        var response = await _client.GetAsync($"/api/users/{_userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.Id.Should().Be(_userId);
        profile.FirstName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_Returns404()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /api/users/me
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyProfile_WithToken_Returns200()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "me@test.com"));

        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.Id.Should().Be(_userId);
    }

    [Fact]
    public async Task GetMyProfile_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PUT /api/users/me
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateProfile_WithValidData_Returns204()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "me@test.com"));

        var response = await _client.PutJsonAsync("/api/users/me", new
        {
            FirstName = "Иван",
            LastName = "Петров",
            Bio = "Люблю природу",
            City = "Москва"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_Then_GetProfile_ReflectsChanges()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "me@test.com"));

        await _client.PutJsonAsync("/api/users/me", new
        {
            FirstName = "Алексей",
            LastName = "Смирнов",
            Bio = "Новое описание",
            City = "Казань"
        });

        var profile = await (await _client.GetAsync($"/api/users/{_userId}"))
            .ReadAsAsync<UserProfileDto>();

        profile.FirstName.Should().Be("Алексей");
        profile.City.Should().Be("Казань");
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PutJsonAsync("/api/users/me", new
        {
            FirstName = "X", LastName = "Y", Bio = (string?)null, City = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PUT /api/users/me/avatar
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAvatar_WithValidUrl_Returns204()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "me@test.com"));

        var response = await _client.PutJsonAsync("/api/users/me/avatar", new
        {
            AvatarUrl = "https://cdn.example.com/avatar.jpg"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // -------------------------------------------------------------------------
    // GET /api/users/leaderboard
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetLeaderboard_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/users/leaderboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
