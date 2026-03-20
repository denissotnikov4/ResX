using MediatR;

namespace ResX.Listings.Application.Commands.DeleteListing;

public record DeleteListingCommand(
    Guid ListingId,
    Guid RequestingUserId) : IRequest<Unit>;
