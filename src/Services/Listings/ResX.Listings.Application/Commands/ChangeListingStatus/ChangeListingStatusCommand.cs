using MediatR;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.Application.Commands.ChangeListingStatus;

public record ChangeListingStatusCommand(
    Guid ListingId,
    Guid RequestingUserId,
    ListingStatus NewStatus) : IRequest<Unit>;
