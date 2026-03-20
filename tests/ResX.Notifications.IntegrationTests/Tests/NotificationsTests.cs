using System.Net;
using FluentAssertions;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Notifications.IntegrationTests.Collections;
using ResX.Notifications.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Notifications.IntegrationTests.Tests;

[Collection(NotificationsCollection.Name)]
public sealed class NotificationsTests : IAsyncLifetime
{
    private readonly NotificationsWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationsTests(NotificationsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithBearerToken(JwtTokenHelper.GenerateAccessToken(_userId, "user@test.com"));
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/notifications
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetNotifications_EmptyState_Returns200WithEmptyItems()
    {
        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<NotificationListResponse>();
        body.Items.Should().BeEmpty();
        body.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_AfterSeedingOne_ReturnsIt()
    {
        await _factory.SeedNotificationAsync(_userId, "Новое объявление", "Опубликовано!");

        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<NotificationListResponse>();
        body.Items.Should().HaveCount(1);
        body.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetNotifications_OnlyUnread_FiltersCorrectly()
    {
        await _factory.SeedNotificationAsync(_userId, "Первое", "Тело");
        await _factory.SeedNotificationAsync(_userId, "Второе", "Тело");

        var response = await _client.GetAsync("/api/notifications?onlyUnread=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<NotificationListResponse>();
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // POST /api/notifications/{id}/read
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MarkAsRead_ExistingNotification_Returns204()
    {
        var notificationId = await _factory.SeedNotificationAsync(_userId);

        var response = await _client.PostAsync($"/api/notifications/{notificationId}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_Returns204()
    {
        // Silently ignored if notification not found or doesn't belong to user
        var response = await _client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsRead_ThenGetNotifications_ShowsReadStatus()
    {
        var notificationId = await _factory.SeedNotificationAsync(_userId);
        await _client.PostAsync($"/api/notifications/{notificationId}/read", null);

        var response = await _client.GetAsync("/api/notifications");
        var body = await response.ReadAsAsync<NotificationListResponse>();

        body.UnreadCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // POST /api/notifications/read-all
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MarkAllAsRead_MultipleNotifications_Returns204()
    {
        await _factory.SeedNotificationAsync(_userId, "Первое", "Тело");
        await _factory.SeedNotificationAsync(_userId, "Второе", "Тело");

        var response = await _client.PostAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAllAsRead_ThenUnreadCountIsZero()
    {
        await _factory.SeedNotificationAsync(_userId, "А", "Тело");
        await _factory.SeedNotificationAsync(_userId, "Б", "Тело");

        await _client.PostAsync("/api/notifications/read-all", null);

        var body = await (await _client.GetAsync("/api/notifications")).ReadAsAsync<NotificationListResponse>();
        body.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkAllAsRead_WithoutToken_Returns401()
    {
        _client.WithoutAuth();
        var response = await _client.PostAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed record NotificationItem(Guid Id, bool IsRead);
    private sealed record NotificationListResponse(List<NotificationItem> Items, int UnreadCount);
}
