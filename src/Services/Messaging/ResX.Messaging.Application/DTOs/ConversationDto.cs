namespace ResX.Messaging.Application.DTOs;

public record ConversationDto(
    Guid Id,
    IReadOnlyList<Guid> Participants,
    Guid? ListingId,
    MessageDto? LastMessage,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime? LastMessageAt);