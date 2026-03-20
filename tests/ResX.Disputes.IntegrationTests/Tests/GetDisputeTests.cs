using System.Net;
using FluentAssertions;
using ResX.Disputes.IntegrationTests.Collections;
using ResX.Disputes.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Disputes.IntegrationTests.Tests;

/// <summary>
/// Tests for GET /api/disputes/{id}
///
/// Rules:
///   - Requires authentication
///   - Returns 200 with DisputeDto on success
///   - Returns 404 if dispute does not exist
///   - Returns 401 without auth
/// </summary>
[Collection(DisputesCollection.Name)]
public sealed class GetDisputeTests : IAsyncLifetime
{
    private readonly DisputesWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _respondentId = Guid.NewGuid();

    public GetDisputeTests(DisputesWebAppFactory factory)
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
    public async Task GetById_ExistingDispute_Returns200WithDto()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var response = await _client.GetAsync($"/api/disputes/{disputeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<DisputeResponse>();
        body.Id.Should().Be(disputeId);
        body.InitiatorId.Should().Be(_userId);
        body.RespondentId.Should().Be(_respondentId);
        body.Status.Should().Be("Open");
        body.Evidences.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_AfterAddingEvidence_ReturnsEvidences()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        await _client.PostJsonAsync($"/api/disputes/{disputeId}/evidence", new
        {
            Description = "Фото товара",
            FileUrls = new[] { "https://cdn.example.com/img.jpg" }
        });

        var response = await _client.GetAsync($"/api/disputes/{disputeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<DisputeResponse>();
        body.Evidences.Should().HaveCount(1);
        body.Evidences[0].Description.Should().Be("Фото товара");
    }

    [Fact]
    public async Task GetById_AsModerator_Returns200()
    {
        var disputeId = await OpenDisputeAsync();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator"));
        var response = await _client.GetAsync($"/api/disputes/{disputeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Not found / auth
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_NonExistentDispute_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var response = await _client.GetAsync($"/api/disputes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithoutAuth_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/disputes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            Reason = "Товар не соответствует описанию"
        });
        var body = await response.ReadAsAsync<DisputeIdResponse>();
        return body.DisputeId;
    }

    private sealed record DisputeIdResponse(Guid DisputeId);
    private sealed record EvidenceResponse(Guid Id, string Description, List<string> FileUrls);
    private sealed record DisputeResponse(
        Guid Id,
        Guid TransactionId,
        Guid InitiatorId,
        Guid RespondentId,
        string Reason,
        string Status,
        string? Resolution,
        DateTime CreatedAt,
        DateTime? ResolvedAt,
        List<EvidenceResponse> Evidences);
}
