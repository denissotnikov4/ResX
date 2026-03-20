using System.Net;
using FluentAssertions;
using ResX.Disputes.IntegrationTests.Collections;
using ResX.Disputes.IntegrationTests.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using Xunit;

namespace ResX.Disputes.IntegrationTests.Tests;

[Collection(DisputesCollection.Name)]
public sealed class DisputesTests : IAsyncLifetime
{
    private readonly DisputesWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _respondentId = Guid.NewGuid();

    public DisputesTests(DisputesWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/disputes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyDisputes_EmptyState_Returns200()
    {
        var response = await _client.GetAsync("/api/disputes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyDisputes_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/disputes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/disputes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OpenDispute_WithValidData_Returns200WithId()
    {
        var response = await _client.PostJsonAsync("/api/disputes", new
        {
            TransactionId = Guid.NewGuid(),
            RespondentId = _respondentId,
            Reason = "Товар не соответствует описанию"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<DisputeIdResponse>();
        body.DisputeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OpenDispute_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/disputes", new
        {
            TransactionId = Guid.NewGuid(),
            RespondentId = _respondentId,
            Reason = "Причина"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyDisputes_AfterOpening_ReturnsIt()
    {
        await _client.PostJsonAsync("/api/disputes", new
        {
            TransactionId = Guid.NewGuid(),
            RespondentId = _respondentId,
            Reason = "Описание проблемы"
        });

        var response = await _client.GetAsync("/api/disputes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // POST /api/disputes/{id}/evidence
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddEvidence_ToOwnDispute_Returns200WithId()
    {
        var disputeId = await OpenDisputeAsync();

        var response = await _client.PostJsonAsync($"/api/disputes/{disputeId}/evidence", new
        {
            Description = "Фото повреждений",
            FileUrls = new[] { "https://cdn.example.com/photo.jpg" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<EvidenceIdResponse>();
        body.EvidenceId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddEvidence_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync($"/api/disputes/{Guid.NewGuid()}/evidence", new
        {
            Description = "Описание",
            FileUrls = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/disputes/open  (requires Moderator role)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOpenDisputes_AsRegularUser_Returns403()
    {
        // Regular "Donor" role — insufficient
        var response = await _client.GetAsync("/api/disputes/open");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOpenDisputes_AsModerator_Returns200()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator"));

        var response = await _client.GetAsync("/api/disputes/open");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // POST /api/disputes/{id}/resolve  (requires Moderator role)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResolveDispute_AsRegularUser_Returns403()
    {
        var disputeId = await OpenDisputeAsync();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com", "Donor"));

        var response = await _client.PostJsonAsync($"/api/disputes/{disputeId}/resolve", new
        {
            Resolution = "Возврат средств"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResolveDispute_AsModerator_Returns204()
    {
        var disputeId = await OpenDisputeAsync();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(Guid.NewGuid(), "mod@test.com", "Moderator"));

        var response = await _client.PostJsonAsync($"/api/disputes/{disputeId}/resolve", new
        {
            Resolution = "Вопрос решён, спор закрыт"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> OpenDisputeAsync()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var response = await _client.PostJsonAsync("/api/disputes", new
        {
            TransactionId = Guid.NewGuid(),
            RespondentId = _respondentId,
            Reason = "Проблема"
        });
        var body = await response.ReadAsAsync<DisputeIdResponse>();
        return body.DisputeId;
    }

    private sealed record DisputeIdResponse(Guid DisputeId);
    private sealed record EvidenceIdResponse(Guid EvidenceId);
}
