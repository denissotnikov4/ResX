using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Users.Application.Commands.UpdateEcoStats;

namespace ResX.Users.Application.IntegrationEvents.TransactionCompleted;

public class TransactionCompletedIntegrationEventHandler
    : IIntegrationEventHandler<TransactionCompletedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionCompletedIntegrationEventHandler> _logger;

    public TransactionCompletedIntegrationEventHandler(
        IMediator mediator,
        ILogger<TransactionCompletedIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(
        TransactionCompletedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "TransactionCompleted received: tx={Tx} donor={Donor} recipient={Recipient} co2={Co2}g waste={Waste}g",
            @event.TransactionId, @event.DonorId, @event.RecipientId, @event.Co2SavedG, @event.WasteSavedG);

        var co2Kg = (decimal)@event.Co2SavedG / 1000m;
        var wasteKg = (decimal)@event.WasteSavedG / 1000m;

        await TryUpdate(
            new UpdateEcoStatsCommand(
                UserId: @event.DonorId,
                ItemsGiftedDelta: 1,
                ItemsReceivedDelta: 0,
                Co2Delta: co2Kg,
                WasteDelta: wasteKg),
            cancellationToken);

        await TryUpdate(
            new UpdateEcoStatsCommand(
                UserId: @event.RecipientId,
                ItemsGiftedDelta: 0,
                ItemsReceivedDelta: 1,
                Co2Delta: 0m,
                WasteDelta: 0m),
            cancellationToken);
    }

    private async Task TryUpdate(UpdateEcoStatsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(command, cancellationToken);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Skipping eco stats update — no profile for {UserId}.", command.UserId);
        }
    }
}
