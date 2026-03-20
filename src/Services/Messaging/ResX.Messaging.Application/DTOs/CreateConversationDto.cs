namespace ResX.Messaging.Application.DTOs;

public record CreateConversationDto(
    Guid RecipientId,
    Guid? ListingId,
    string? InitialMessage);