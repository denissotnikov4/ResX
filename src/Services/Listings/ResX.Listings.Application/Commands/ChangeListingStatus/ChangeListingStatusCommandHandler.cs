using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Listings.Application.IntegrationEvents;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Commands.ChangeListingStatus;

public class ChangeListingStatusCommandHandler : IRequestHandler<ChangeListingStatusCommand, Unit>
{
    private readonly IListingRepository _listingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly ICacheService _cache;
    private readonly ILogger<ChangeListingStatusCommandHandler> _logger;

    public ChangeListingStatusCommandHandler(
        IListingRepository listingRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator,
        ICacheService cache,
        ILogger<ChangeListingStatusCommandHandler> logger)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(ChangeListingStatusCommand request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        var previousStatus = listing.Status;
        listing.ChangeStatus(request.NewStatus);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        await _cache.SetAsync("listings:version", version + 1, TimeSpan.FromDays(365), cancellationToken);

        foreach (var domainEvent in listing.DomainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
        listing.ClearDomainEvents();

        await _eventBus.PublishAsync(new ListingStatusChangedIntegrationEvent(
            listing.Id,
            listing.DonorId,
            previousStatus.ToString(),
            request.NewStatus.ToString()), cancellationToken);

        _logger.LogInformation("Listing {ListingId} status changed to {Status}.", request.ListingId, request.NewStatus);
        return Unit.Value;
    }
}
