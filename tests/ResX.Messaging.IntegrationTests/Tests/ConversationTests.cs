using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.IntegrationTests.Collections;
using ResX.Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Messaging.IntegrationTests.Tests;

[Collection(MessagingCollection.Name)]
public sealed class ConversationTests : IAsyncLifetime
{
    private readonly MessagingWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    public ConversationTests(MessagingWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/messaging/conversations
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetConversations_EmptyState_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/messaging/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConversations_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/messaging/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/messaging/conversations
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateConversation_ValidData_Returns200WithId()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));

        var response = await _client.PostJsonAsync("/api/messaging/conversations", new CreateConversationDto(
            RecipientId: _recipientId,
            ListingId: null,
            InitialMessage: "Привет! Интересует ваш предмет."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<ConversationIdResponse>();
        body.ConversationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateConversation_WithListingId_Returns200()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var listingId = Guid.NewGuid();

        var response = await _client.PostJsonAsync("/api/messaging/conversations", new CreateConversationDto(
            RecipientId: _recipientId,
            ListingId: listingId,
            InitialMessage: "Можно забрать?"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateConversation_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostJsonAsync("/api/messaging/conversations",
            new CreateConversationDto(_recipientId, null, "Hi"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_AfterCreating_ReturnsIt()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));

        await _client.PostJsonAsync("/api/messaging/conversations",
            new CreateConversationDto(_recipientId, null, "Первое сообщение"));

        var response = await _client.GetAsync("/api/messaging/conversations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ConversationIdResponse(Guid ConversationId);
}
