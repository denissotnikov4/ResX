using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Listings.Application.IntegrationEvents;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.ValueObjects;

namespace ResX.Listings.Application.Commands.CreateListing;

public class CreateListingCommandHandler : IRequestHandler<CreateListingCommand, Guid>
{
    private readonly IListingRepository _listingRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateListingCommandHandler> _logger;

    public CreateListingCommandHandler(
        IListingRepository listingRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator,
        ICacheService cache,
        ILogger<CreateListingCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (!category.IsActive)
            throw new DomainException("Cannot create a listing in an inactive category.");

        var location = Location.Create(request.City, request.District, request.Latitude, request.Longitude);

        var listing = Listing.Create(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Condition,
            request.TransferType,
            request.TransferMethod,
            location,
            request.DonorId,
            request.Tags);

        await _listingRepository.AddAsync(listing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        await _cache.SetAsync("listings:version", version + 1, TimeSpan.FromDays(365), cancellationToken);

        foreach (var domainEvent in listing.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        listing.ClearDomainEvents();

        await _eventBus.PublishAsync(new ListingCreatedIntegrationEvent(
            listing.Id,
            listing.DonorId,
            listing.Title,
            category.Name,
            listing.Location.City), cancellationToken);

        _logger.LogInformation("Listing {ListingId} created by donor {DonorId}.", listing.Id, request.DonorId);

        return listing.Id;
    }
}
