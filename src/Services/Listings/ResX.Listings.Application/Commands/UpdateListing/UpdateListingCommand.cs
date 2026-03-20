using MediatR;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.Application.Commands.UpdateListing;

public record UpdateListingCommand(
    Guid ListingId,
    Guid RequestingUserId,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    Guid? ParentCategoryId,
    ItemCondition Condition,
    TransferType TransferType,
    TransferMethod TransferMethod,
    string City,
    string? District,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<string>? Tags) : IRequest<Unit>;
