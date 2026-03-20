using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Transactions.Application.DTOs;
using ResX.Transactions.Domain.Enums;
using ResX.Transactions.IntegrationTests.Collections;
using ResX.Transactions.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Transactions.IntegrationTests.Tests;

[Collection(TransactionsCollection.Name)]
public sealed class CreateTransactionTests : IAsyncLifetime
{
    private readonly TransactionsWebAppFactory _factory;
    private readonly HttpClient _client;

    // Simulate two distinct users
    private readonly Guid _donorId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();
    private readonly Guid _listingId = Guid.NewGuid();

    public CreateTransactionTests(TransactionsWebAppFactory factory)
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
    public async Task CreateTransaction_WithValidData_Returns201WithId()
    {
        // Arrange — recipient initiates request for donor's listing
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var request = new
        {
            ListingId = _listingId,
            DonorId = _donorId,
            Type = TransactionType.Gift,
            Notes = "Хочу забрать диван"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.ReadAsAsync<IdResponse>();
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_Returns201_LocationHeaderSet()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var response = await _client.PostJsonAsync("/api/transactions", ValidRequest());

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/transactions/");
    }

    [Fact]
    public async Task CreateTransaction_ExchangeType_Returns201()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var request = ValidRequest() with { Type = TransactionType.Exchange };
        var response = await _client.PostJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTransaction_WithNotes_Returns201()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var request = ValidRequest() with { Notes = "Заберу в понедельник" };
        var response = await _client.PostJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // -------------------------------------------------------------------------
    // Self-transaction prevention
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTransaction_RecipientIsDonor_Returns400()
    {
        // The caller (recipient from JWT) tries to request their own listing
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_donorId, "donor@test.com"));

        var request = new
        {
            ListingId = _listingId,
            DonorId = _donorId,    // same as caller — self-transaction
            Type = TransactionType.Gift,
            Notes = (string?)null
        };

        var response = await _client.PostJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Authentication required
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTransaction_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/transactions", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTransaction_WithExpiredToken_Returns401()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateExpiredToken(_recipientId, "recipient@test.com"));

        var response = await _client.PostJsonAsync("/api/transactions", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GetById
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_AfterCreate_Returns200WithCorrectData()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var createResponse = await _client.PostJsonAsync("/api/transactions", ValidRequest());
        var created = await createResponse.ReadAsAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/transactions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(created.Id.ToString());
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyTransactions_Returns200()
    {
        _client.WithBearerToken(
            JwtTokenHelper.GenerateAccessToken(_recipientId, "recipient@test.com"));

        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private CreateTransactionRequest ValidRequest() => new(_listingId, _donorId, TransactionType.Gift, null);

    private sealed record IdResponse(Guid Id);
}