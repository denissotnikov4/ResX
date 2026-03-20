namespace ResX.Charity.Application.DTOs;

public record CreateCharityRequestDto(
    string Title,
    string Description,
    DateTime? DeadlineDate,
    IReadOnlyList<CreateRequestedItemDto> Items);
