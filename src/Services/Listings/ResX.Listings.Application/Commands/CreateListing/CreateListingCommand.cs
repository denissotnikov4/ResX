using MediatR;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.Application.Commands.CreateListing;

public record CreateListingCommand(
    Guid DonorId,
    string Title,
    string Description,
    Guid CategoryId,
    int WeightGrams,
    ItemCondition Condition,
    TransferType TransferType,
    TransferMethod TransferMethod,
    string City,
    string? District,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<string>? Tags) : IRequest<Guid>;
