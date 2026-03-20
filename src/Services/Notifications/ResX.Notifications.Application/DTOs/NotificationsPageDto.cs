namespace ResX.Notifications.Application.DTOs;

public record NotificationsPageDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);
