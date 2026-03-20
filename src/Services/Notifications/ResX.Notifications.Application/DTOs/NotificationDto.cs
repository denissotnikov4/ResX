namespace ResX.Notifications.Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    object? Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);
