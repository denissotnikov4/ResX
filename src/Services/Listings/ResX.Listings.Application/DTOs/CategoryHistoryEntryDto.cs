namespace ResX.Listings.Application.DTOs;

public record CategoryHistoryEntryDto(
    Guid Id,
    Guid CategoryId,
    Guid ChangedByUserId,
    string ChangeType,
    string? OldValuesJson,
    string? NewValuesJson,
    DateTime ChangedAt);
