using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateListingCommandHandler> _logger;

    public CreateListingCommandHandler(
        IListingRepository listingRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator,
        ICacheService cache,
        ILogger<CreateListingCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.CategoryId, request.CategoryName, request.ParentCategoryId);
        var location = Location.Create(request.City, request.District, request.Latitude, request.Longitude);

        var listing = Listing.Create(
            request.Title,
            request.Description,
            category,
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
            listing.Category.Name,
            listing.Location.City), cancellationToken);

        _logger.LogInformation("Listing {ListingId} created by donor {DonorId}.", listing.Id, request.DonorId);

        return listing.Id;
    }
}
