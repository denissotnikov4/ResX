namespace ResX.Charity.Application.DTOs;

public record CharityRequestDto(
    Guid Id,
    Guid OrganizationId,
    string Title,
    string Description,
    string Status,
    IReadOnlyList<RequestedItemDto> RequestedItems,
    DateTime? DeadlineDate,
    DateTime CreatedAt);