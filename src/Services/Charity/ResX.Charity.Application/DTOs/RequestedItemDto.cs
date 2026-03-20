namespace ResX.Charity.Application.DTOs;

public record RequestedItemDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    int QuantityNeeded,
    int QuantityReceived,
    string Condition);