namespace ResX.Charity.Application.DTOs;

public record CreateRequestedItemDto(
    Guid CategoryId,
    string CategoryName,
    int QuantityNeeded,
    string Condition);