using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Transactions.Domain.Enums;
using ResX.Transactions.IntegrationTests.Collections;
using ResX.Transactions.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Transactions.IntegrationTests.Tests;

/// <summary>
/// Tests the full transaction state machine:
/// Pending → DonorAgreed → Completed
/// Pending → Cancelled
/// </summary>
[Collection(TransactionsCollection.Name)]
public sealed class TransactionLifecycleTests : IAsyncLifetime
{
    private readonly TransactionsWebAppFactory _factory;
    private readonly HttpClient _client;

    private readonly Guid _donorId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    public TransactionLifecycleTests(TransactionsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Happy path: Pending → DonorAgreed → Completed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FullFlow_DonorAgrees_ThenRecipientConfirms_TransactionCompleted()
    {
        // Step 1: Recipient creates the transaction (Pending)
        var txId = await CreateTransactionAsRecipient();

        // Step 2: Donor agrees (Pending → DonorAgreed)
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var agreeResponse = await _client.PostAsync($"/api/transactions/{txId}/agree", null);
        agreeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Donor should be able to agree to the transaction");

        // Step 3: Recipient confirms receipt (DonorAgreed → Completed)
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        var confirmResponse = await _client.PostAsync($"/api/transactions/{txId}/confirm-receipt", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "Recipient should be able to confirm receipt");
    }

    [Fact]
    public async Task Agree_ByDonor_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/agree", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ConfirmReceipt_ByRecipient_AfterDonorAgree_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        // Donor agrees first
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/agree", null);

        // Recipient confirms
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/confirm-receipt", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // -------------------------------------------------------------------------
    // Cancel flow
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_ByRecipient_WhilePending_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_ByDonor_WhilePending_Returns204()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_AfterAlreadyCancelled_Returns400()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));
        await _client.PostAsync($"/api/transactions/{txId}/cancel", null);

        // Second cancel attempt
        var response = await _client.PostAsync($"/api/transactions/{txId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Unauthorized state transitions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Agree_ByUnrelatedUser_Returns403OrNotFound()
    {
        var txId = await CreateTransactionAsRecipient();
        var stranger = Guid.NewGuid();

        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(stranger, "stranger@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{txId}/agree", null);

        ((int)response.StatusCode).Should().BeOneOf(403, 404);
    }

    [Fact]
    public async Task Agree_WithoutAuth_Returns401()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithoutAuth();
        var response = await _client.PostAsync($"/api/transactions/{txId}/agree", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmReceipt_WithoutAuth_Returns401()
    {
        var txId = await CreateTransactionAsRecipient();

        _client.WithoutAuth();
        var response = await _client.PostAsync($"/api/transactions/{txId}/confirm-receipt", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Non-existent transaction
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Agree_NonExistentTransaction_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{Guid.NewGuid()}/agree", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_NonExistentTransaction_Returns404()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));
        var response = await _client.PostAsync($"/api/transactions/{Guid.NewGuid()}/cancel", null);

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
