using System.Net;
using FluentAssertions;
using ResX.Identity.Application.Commands.RegisterUser;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Domain.Enums;
using ResX.Identity.IntegrationTests.Collections;
using ResX.Identity.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Identity.IntegrationTests.Tests;

[Collection(IdentityCollection.Name)]
public sealed class RegisterTests : IAsyncLifetime
{
    private readonly IdentityWebAppFactory _factory;
    private readonly HttpClient _client;

    public RegisterTests(IdentityWebAppFactory factory)
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
    public async Task Register_WithValidDonor_Returns201WithTokens()
    {
        // Arrange
        var command = ValidRegisterCommand();

        // Act
        var response = await _client.PostJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tokens = await response.ReadAsAsync<TokensDto>();
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithRecipientRole_Returns201()
    {
        var command = ValidRegisterCommand() with { Role = UserRole.Recipient };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithPhoneNumber_Returns201()
    {
        var command = ValidRegisterCommand() with { Phone = "+79001234567" };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // -------------------------------------------------------------------------
    // Conflict — duplicate email
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        // Arrange — register the same email twice
        var command = ValidRegisterCommand();
        await _client.PostJsonAsync("/api/auth/register", command);

        // Act
        var response = await _client.PostJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Validation errors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_EmptyEmail_Returns422()
    {
        var command = ValidRegisterCommand() with { Email = string.Empty };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns422()
    {
        var command = ValidRegisterCommand() with { Email = "not-an-email" };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns422()
    {
        var command = ValidRegisterCommand() with { Password = "123" };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_EmptyFirstName_Returns422()
    {
        var command = ValidRegisterCommand() with { FirstName = string.Empty };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_EmptyLastName_Returns422()
    {
        var command = ValidRegisterCommand() with { LastName = string.Empty };

        var response = await _client.PostJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static RegisterUserCommand ValidRegisterCommand() => new(
        Email: FakerExtensions.RandomEmail(),
        Phone: null,
        Password: FakerExtensions.RandomPassword(),
        FirstName: FakerExtensions.RandomFirstName(),
        LastName: FakerExtensions.RandomLastName(),
        Role: UserRole.Donor);
}
