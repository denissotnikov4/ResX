using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Commands.DeleteListing;

public class DeleteListingCommandHandler : IRequestHandler<DeleteListingCommand, Unit>
{
    private readonly IListingRepository _listingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<DeleteListingCommandHandler> _logger;

    public DeleteListingCommandHandler(
        IListingRepository listingRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<DeleteListingCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        if (listing.DonorId != request.RequestingUserId)
            throw new ForbiddenException("You can only delete your own listings.");

        listing.Cancel();
        await _listingRepository.DeleteAsync(request.ListingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        await _cache.SetAsync("listings:version", version + 1, TimeSpan.FromDays(365), cancellationToken);

        _logger.LogInformation("Listing {ListingId} deleted.", request.ListingId);
        return Unit.Value;
    }
}
