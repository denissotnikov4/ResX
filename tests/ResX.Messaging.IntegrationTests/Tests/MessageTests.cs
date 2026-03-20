using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.IntegrationTests.Collections;
using ResX.Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Messaging.IntegrationTests.Tests;

[Collection(MessagingCollection.Name)]
public sealed class MessageTests : IAsyncLifetime
{
    private readonly MessagingWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    public MessageTests(MessagingWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> CreateConversationAsync()
    {
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
        var response = await _client.PostJsonAsync("/api/messaging/conversations",
            new CreateConversationDto(_recipientId, null, "Привет!"));
        var body = await response.ReadAsAsync<ConversationIdResponse>();
        return body.ConversationId;
    }

    // -------------------------------------------------------------------------
    // GET /api/messaging/conversations/{id}/messages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMessages_ForOwnConversation_Returns200()
    {
        var conversationId = await CreateConversationAsync();

        var response = await _client.GetAsync($"/api/messaging/conversations/{conversationId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync($"/api/messaging/conversations/{Guid.NewGuid()}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/messaging/conversations/{id}/messages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendMessage_ToOwnConversation_Returns200WithMessage()
    {
        var conversationId = await CreateConversationAsync();

        var response = await _client.PostJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages",
            new SendMessageDto(conversationId, "Это тестовое сообщение"));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
    }

    [Fact]
    public async Task SendMessage_ThenGetMessages_ContainsIt()
    {
        var conversationId = await CreateConversationAsync();

        await _client.PostJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages",
            new SendMessageDto(conversationId, "Привет ещё раз"));

        var listResponse = await _client.GetAsync($"/api/messaging/conversations/{conversationId}/messages");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // POST /api/messaging/conversations/{id}/read
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MarkAsRead_OwnConversation_Returns204()
    {
        var conversationId = await CreateConversationAsync();

        var response = await _client.PostAsync(
            $"/api/messaging/conversations/{conversationId}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsRead_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostAsync(
            $"/api/messaging/conversations/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ConversationIdResponse(Guid ConversationId);
}
