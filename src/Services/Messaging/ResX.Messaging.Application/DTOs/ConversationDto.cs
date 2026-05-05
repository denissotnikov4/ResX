namespace ResX.Messaging.Application.DTOs;

public record ConversationDto(
    Guid Id,
    IReadOnlyList<ParticipantSummaryDto> Participants,
    ParticipantSummaryDto? Counterparty,
    Guid? ListingId,
    ListingSummaryDto? Listing,
    MessageDto? LastMessage,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime? LastMessageAt);
