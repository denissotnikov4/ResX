namespace ResX.Messaging.Application.DTOs;

public record ParticipantSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? AvatarUrl);
