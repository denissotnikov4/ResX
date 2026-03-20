using MediatR;

namespace ResX.Listings.Application.Commands.AddListingPhoto;

public record AddListingPhotoCommand(
    Guid ListingId,
    Guid RequestingUserId,
    string Url,
    int DisplayOrder) : IRequest<Guid>;
