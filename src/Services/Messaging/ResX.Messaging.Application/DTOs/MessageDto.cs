namespace ResX.Messaging.Application.DTOs;

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    DateTime SentAt,
    bool IsRead);