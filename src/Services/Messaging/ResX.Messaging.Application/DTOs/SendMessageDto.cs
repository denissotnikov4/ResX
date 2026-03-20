namespace ResX.Messaging.Application.DTOs;

public record SendMessageDto(
    Guid ConversationId,
    string Content);