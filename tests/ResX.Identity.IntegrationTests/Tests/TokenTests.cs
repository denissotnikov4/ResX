using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ResX.Identity.Application.Commands.LoginUser;
using ResX.Identity.Application.Commands.RegisterUser;
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

    private static readonly WebApplicationFactoryClientOptions HttpsOptions = new()
    {
        BaseAddress = new Uri("https://localhost")
    };

    public TokenTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(HttpsOptions);
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Refresh token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshToken_WithValidCookie_Returns204WithNewTokens()
    {
        // Arrange — register and login (cookies are set automatically)
        var originalTokens = await RegisterAndLogin();

        // Act — refresh endpoint reads refreshToken from cookie
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var newAccessToken = response.GetCookieValue("accessToken");
        var newRefreshToken = response.GetCookieValue("refreshToken");
        newAccessToken.Should().NotBeNullOrWhiteSpace();
        newRefreshToken.Should().NotBeNullOrWhiteSpace();
        // New tokens should differ from original
        newAccessToken.Should().NotBe(originalTokens.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_WithoutCookie_Returns401()
    {
        using var freshClient = _factory.CreateClient(HttpsOptions);
        var response = await freshClient.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_AfterRevoke_Returns401()
    {
        // Arrange — register & login, save old refresh token
        var tokens = await RegisterAndLogin();
        var oldRefreshToken = tokens.RefreshToken;

        // Use the refresh token once (revokes the original, sets new cookies)
        await _client.PostAsync("/api/auth/refresh", null);

        // Act — try to use the old refresh token via a fresh client (no cookie jar)
        using var noCookiesClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false
            });
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={oldRefreshToken}");
        var response = await noCookiesClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Logout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_WithValidCookies_Returns204()
    {
        // Arrange — register & login (sets accessToken + refreshToken cookies)
        await RegisterAndLogin();

        // Act — logout reads both cookies automatically
        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_Returns401()
    {
        using var freshClient = _factory.CreateClient(HttpsOptions);
        var response = await freshClient.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefreshToken_Returns401()
    {
        // Arrange — register, login, then logout (clears cookies + revokes token)
        await RegisterAndLogin();
        await _client.PostAsync("/api/auth/logout", null);

        // Act — try to refresh (cookies were cleared by logout)
        var response = await _client.PostAsync("/api/auth/refresh", null);

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
        await RegisterAndLogin(email, oldPassword);

        var response = await _client.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = oldPassword, NewPassword = "NewSecurePass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_Returns400OrForbidden()
    {
        var email = FakerExtensions.RandomEmail();
        await RegisterAndLogin(email, "CorrectPass1!");

        var response = await _client.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = "WrongPass1!", NewPassword = "NewSecurePass1!" });

        ((int)response.StatusCode).Should().BeOneOf(400, 403);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_Returns401()
    {
        using var freshClient = _factory.CreateClient(HttpsOptions);
        var response = await freshClient.PutJsonAsync("/api/auth/change-password",
            new { OldPassword = "Old1!", NewPassword = "New1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private record CookieTokens(string AccessToken, string RefreshToken);

    private async Task<CookieTokens> RegisterAndLogin(
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

        return new CookieTokens(
            loginResponse.GetCookieValue("accessToken")!,
            loginResponse.GetCookieValue("refreshToken")!);
    }
}
