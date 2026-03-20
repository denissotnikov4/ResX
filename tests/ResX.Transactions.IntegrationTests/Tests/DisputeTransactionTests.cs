using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Transactions.Domain.Enums;
using ResX.Transactions.IntegrationTests.Collections;
using ResX.Transactions.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Transactions.IntegrationTests.Tests;

/// <summary>
/// Tests for POST /api/transactions/{id}/dispute
///
/// State machine rules (from Transaction.Dispute):
///   - Allowed in: Pending, DonorAgreed
///   - Forbidden in: Completed, Cancelled
///   - Only participants (donor or recipient) can dispute
/// </summary>
[Collection(TransactionsCollection.Name)]
public sealed class DisputeTransactionTests : IAsyncLifetime
{
    private readonly TransactionsWebAppFactory _factory;
    private readonly HttpClient _client;

    private readonly Guid _donorId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    public DisputeTransactionTests(TransactionsWebAppFactory factory)
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
    public async Task Dispute_WhilePending_ByRecipient_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "recipient can open a dispute while transaction is Pending");
    }

    [Fact]
    public async Task Dispute_WhilePending_ByDonor_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "donor can open a dispute while transaction is Pending");
    }

    [Fact]
    public async Task Dispute_WhileDonorAgreed_ByRecipient_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        // Move to DonorAgreed
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/agree", null);

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "recipient can open a dispute in DonorAgreed status");
    }

    // -------------------------------------------------------------------------
    // Forbidden status transitions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Dispute_AfterCompleted_Returns400()
    {
        var txId = await CreateTransactionAsRecipient();

        // Complete the transaction
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/agree", null);
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/confirm-receipt", null);

        // Attempt to dispute a completed transaction
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "cannot dispute a completed transaction");
    }

    [Fact]
    public async Task Dispute_AfterCancelled_Returns400()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/cancel", null);

        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "cannot dispute a cancelled transaction");
    }

    [Fact]
    public async Task Dispute_AlreadyDisputed_Returns400()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        // Second dispute attempt on an already Disputed transaction
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "cannot open a dispute on an already disputed transaction");
    }

    // -------------------------------------------------------------------------
    // Authorization checks
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Dispute_ByUnrelatedUser_Returns403()
    {
        var txId = await CreateTransactionAsRecipient();
        var stranger = Guid.NewGuid();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(stranger, "stranger@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a user who is not a participant cannot dispute the transaction");
    }

    [Fact]
    public async Task Dispute_WithoutAuth_Returns401()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithoutAuth();
        var response = await _client.PostAsync($"/api/transactions/{txId}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Dispute_NonExistentTransaction_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{Guid.NewGuid()}/dispute", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateTransactionAsRecipient()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var request = new
        {
            ListingId = Guid.NewGuid(),
            DonorId = _donorId,
            Type = TransactionType.Gift,
            Notes = (string?)null
        };

        var response = await _client.PostJsonAsync("/api/transactions", request);
        response.EnsureSuccessStatusCode();
        return (await response.ReadAsAsync<IdResponse>()).Id;
    }

    private sealed record IdResponse(Guid Id);
}
