using System.Net;
using FluentAssertions;
using ResX.Identity.Application.Commands.LoginUser;
using ResX.Identity.Application.Commands.RegisterUser;
using ResX.Identity.Application.DTOs;
using ResX.Identity.IntegrationTests.Collections;
using ResX.Identity.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Identity.IntegrationTests.Tests;

[Collection(IdentityCollection.Name)]
public sealed class LoginTests : IAsyncLifetime
{
    private readonly IdentityWebAppFactory _factory;
    private readonly HttpClient _client;

    public LoginTests(IdentityWebAppFactory factory)
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
    public async Task Login_WithCorrectCredentials_Returns200WithTokens()
    {
        // Arrange — create a user first
        var email = FakerExtensions.RandomEmail();
        var password = FakerExtensions.RandomPassword();
        await RegisterUser(email, password);

        // Act
        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand(email, password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.ReadAsAsync<TokensDto>();
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ReturnsAccessTokenWithCorrectClaims()
    {
        var email = FakerExtensions.RandomEmail();
        var password = FakerExtensions.RandomPassword();
        await RegisterUser(email, password);

        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.ReadAsAsync<TokensDto>();
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();

        // The token should be a valid JWT we can parse
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokens.AccessToken);

        jwt.Claims.Should().Contain(c => c.Type == "email" || c.Value == email.ToLowerInvariant());
    }

    // -------------------------------------------------------------------------
    // Authentication failures
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WrongPassword_Returns400()
    {
        var email = FakerExtensions.RandomEmail();
        await RegisterUser(email, "CorrectPass1!");

        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand(email, "WrongPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns400()
    {
        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand("nobody@example.com", "SomePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_EmptyLogin_Returns422()
    {
        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand(string.Empty, "SomePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns422()
    {
        var response = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand("user@example.com", string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<TokensDto> RegisterUser(string email, string password)
    {
        var response = await _client.PostJsonAsync("/api/auth/register",
            new RegisterUserCommand(email, null, password,
                FakerExtensions.RandomFirstName(),
                FakerExtensions.RandomLastName()));
        return await response.ReadAsAsync<TokensDto>();
    }
}
