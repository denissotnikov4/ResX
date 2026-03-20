using System.Net;
using FluentAssertions;
using ResX.Disputes.IntegrationTests.Collections;
using ResX.Disputes.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Disputes.IntegrationTests.Tests;

/// <summary>
/// Tests for POST /api/disputes/{id}/close
///
/// Rules:
///   - Requires Moderator or Admin role
///   - Returns 204 on success (any non-closed status → Closed)
///   - Returns 404 if dispute does not exist
///   - Returns 401 without auth, 403 for regular users
/// </summary>
[Collection(DisputesCollection.Name)]
public sealed class CloseDisputeTests : IAsyncLifetime
{
    private readonly DisputesWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _respondentId = Guid.NewGuid();

    public CloseDisputeTests(DisputesWebAppFactory factory)
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
    public async Task Close_OpenDispute_AsModerator_Returns204()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator"));
        var response = await _client.PostAsync($"/api/disputes/{disputeId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "moderator can close an open dispute");
    }

    [Fact]
    public async Task Close_OpenDispute_AsAdmin_Returns204()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "admin@test.com", "Admin"));
        var response = await _client.PostAsync($"/api/disputes/{disputeId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "admin can close an open dispute");
    }

    [Fact]
    public async Task Close_ResolvedDispute_AsModerator_Returns204()
    {
        var disputeId = await OpenDisputeAsync();

        var mod = JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator");
        _client.WithBearerToken(mod);
        await _client.PostJsonAsync($"/api/disputes/{disputeId}/resolve", new { Resolution = "Решение принято" });

        var response = await _client.PostAsync($"/api/disputes/{disputeId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "moderator can close a resolved dispute");
    }

    // -------------------------------------------------------------------------
    // Authorization
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Close_AsRegularUser_Returns403()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "user@test.com", "Donor"));
        var response = await _client.PostAsync($"/api/disputes/{disputeId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Close_WithoutAuth_Returns401()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithoutAuth();
        var response = await _client.PostAsync($"/api/disputes/{disputeId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Close_NonExistentDispute_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator"));
        var response = await _client.PostAsync($"/api/disputes/{Guid.NewGuid()}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private async Task<Guid> OpenDisputeAsync()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var response = await _client.PostJsonAsync("/api/disputes", new
        {
            TransactionId = Guid.NewGuid(),
            RespondentId = _respondentId,
            Reason = "Проблема с товаром"
        });
        var body = await response.ReadAsAsync<DisputeIdResponse>();
        return body.DisputeId;
    }

    private sealed record DisputeIdResponse(Guid DisputeId);
}
