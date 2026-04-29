using ResX.Listings.Domain.Enums;

namespace ResX.Listings.Application.DTOs;

public record CreateListingDto(
    string Title,
    string Description,
    Guid CategoryId,
    ItemCondition Condition,
    TransferType TransferType,
    TransferMethod TransferMethod,
    string City,
    string? District,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<string>? Tags);
