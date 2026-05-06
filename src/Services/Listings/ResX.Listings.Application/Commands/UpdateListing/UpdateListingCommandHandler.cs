using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.ValueObjects;

namespace ResX.Listings.Application.Commands.UpdateListing;

public class UpdateListingCommandHandler : IRequestHandler<UpdateListingCommand, Unit>
{
    private readonly IListingRepository _listingRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateListingCommandHandler> _logger;

    public UpdateListingCommandHandler(
        IListingRepository listingRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<UpdateListingCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        if (listing.DonorId != request.RequestingUserId)
            throw new ForbiddenException("You can only update your own listings.");

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (!category.IsActive)
            throw new DomainException("Cannot move a listing to an inactive category.");

        var location = Location.Create(request.City, request.District, request.Latitude, request.Longitude);

        listing.Update(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Condition,
            request.TransferType,
            request.TransferMethod,
            location,
            request.WeightGrams,
            category.Co2SavedPer100GramsG,
            category.WasteSavedPer100GramsG,
            request.Tags);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        await _cache.SetAsync("listings:version", version + 1, TimeSpan.FromDays(365), cancellationToken);

        _logger.LogInformation("Listing {ListingId} updated.", request.ListingId);
        return Unit.Value;
    }
}
