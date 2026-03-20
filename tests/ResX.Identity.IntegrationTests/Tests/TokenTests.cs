using System.Net;
using FluentAssertions;
using ResX.Identity.Application.Commands.LoginUser;
using ResX.Identity.Application.Commands.Logout;
using ResX.Identity.Application.Commands.RefreshToken;
using ResX.Identity.Application.Commands.RegisterUser;
using ResX.Identity.Application.DTOs;
using ResX.Identity.IntegrationTests.Collections;
using ResX.Identity.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Identity.IntegrationTests.Tests;

[Collection(IdentityCollection.Name)]
public sealed class TokenTests : IAsyncLifetime
{
    private readonly IdentityWebAppFactory _factory;
    private readonly HttpClient _client;

    public TokenTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Refresh token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_Returns200WithNewTokens()
    {
        // Arrange — register and get tokens
        var tokens = await RegisterAndLogin();

        // Act
        var response = await _client.PostJsonAsync("/api/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await response.ReadAsAsync<TokensDto>();
        newTokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        newTokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        // New tokens should differ from original
        newTokens.AccessToken.Should().NotBe(tokens.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        var response = await _client.PostJsonAsync("/api/auth/refresh",
            new RefreshTokenCommand("this-is-not-a-valid-refresh-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_AfterRevoke_Returns401()
    {
        // Arrange — log in, get tokens, then use refresh to get new tokens (revokes old)
        var tokens = await RegisterAndLogin();

        // Use the refresh token once (this revokes the original)
        await _client.PostJsonAsync("/api/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));

        // Act — try to use the original refresh token again (should be revoked)
        var response = await _client.PostJsonAsync("/api/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Logout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        var tokens = await RegisterAndLogin();
        _client.WithBearerToken(tokens.AccessToken);

        var response = await _client.PostJsonAsync("/api/auth/logout",
            new LogoutCommand(tokens.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/auth/logout",
            new LogoutCommand("some-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefreshToken_Returns401()
    {
        // Arrange — log in and then log out
        var tokens = await RegisterAndLogin();
        _client.WithBearerToken(tokens.AccessToken);

        await _client.PostJsonAsync("/api/auth/logout",
            new LogoutCommand(tokens.RefreshToken));

        // Act — try to use the refresh token after logout
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // ChangePassword
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_Returns204()
    {
        var email = FakerExtensions.RandomEmail();
        var oldPassword = FakerExtensions.RandomPassword();
        var tokens = await RegisterAndLogin(email, oldPassword);
        _client.WithBearerToken(tokens.AccessToken);

        var response = await _client.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = oldPassword, NewPassword = "NewSecurePass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_Returns400OrForbidden()
    {
        var email = FakerExtensions.RandomEmail();
        var tokens = await RegisterAndLogin(email, "CorrectPass1!");
        _client.WithBearerToken(tokens.AccessToken);

        var response = await _client.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = "WrongPass1!", NewPassword = "NewSecurePass1!" });

        ((int)response.StatusCode).Should().BeOneOf(400, 403);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = "Old1!", NewPassword = "New1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<TokensDto> RegisterAndLogin(
        string? email = null, string? password = null)
    {
        email ??= FakerExtensions.RandomEmail();
        password ??= FakerExtensions.RandomPassword();

        await _client.PostJsonAsync("/api/auth/register",
            new RegisterUserCommand(email, null, password,
                FakerExtensions.RandomFirstName(),
                FakerExtensions.RandomLastName()));

        var loginResponse = await _client.PostJsonAsync("/api/auth/login",
            new LoginUserCommand(email, password));

        return await loginResponse.ReadAsAsync<TokensDto>();
    }
}
