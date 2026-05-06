using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Users.Application.Commands.UpdateEcoStats;

namespace ResX.Users.Application.IntegrationEvents.ListingCreated;

public class ListingCreatedIntegrationEventHandler : IIntegrationEventHandler<ListingCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ListingCreatedIntegrationEventHandler> _logger;

    public ListingCreatedIntegrationEventHandler(
        IMediator mediator,
        ILogger<ListingCreatedIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(
        ListingCreatedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ListingCreated received: listing={ListingId} donor={DonorId} co2={Co2G}g waste={WasteG}g",
            @event.ListingId, @event.DonorId, @event.Co2SavedG, @event.WasteSavedG);

        // Convert grams → kg for stats (decimal precision).
        var co2Kg = (decimal)@event.Co2SavedG / 1000m;
        var wasteKg = (decimal)@event.WasteSavedG / 1000m;

        try
        {
            await _mediator.Send(
                new UpdateEcoStatsCommand(
                    UserId: @event.DonorId,
                    ItemsGiftedDelta: 1,
                    ItemsReceivedDelta: 0,
                    Co2Delta: co2Kg,
                    WasteDelta: wasteKg),
                cancellationToken);
        }
        catch (NotFoundException)
        {
            // UserProfile not yet provisioned (UserRegistered handler hasn't run, or race).
            // Skip silently — eco impact for very-early-account listings will be lost,
            // which is acceptable for an MVP.
            _logger.LogWarning(
                "Skipping eco stats update — no profile for donor {DonorId} yet.",
                @event.DonorId);
        }
    }
}
