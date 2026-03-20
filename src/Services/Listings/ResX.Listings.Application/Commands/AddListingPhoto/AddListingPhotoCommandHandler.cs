using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Commands.AddListingPhoto;

public class AddListingPhotoCommandHandler : IRequestHandler<AddListingPhotoCommand, Guid>
{
    private readonly IListingRepository _listingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddListingPhotoCommandHandler> _logger;

    public AddListingPhotoCommandHandler(
        IListingRepository listingRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddListingPhotoCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddListingPhotoCommand request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        if (listing.DonorId != request.RequestingUserId)
            throw new ForbiddenException("You can only add photos to your own listings.");

        var photo = listing.AddPhoto(request.Url, request.DisplayOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Photo added to listing {ListingId}.", request.ListingId);
        return photo.Id;
    }
}
