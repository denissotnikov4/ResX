using MediatR;
using Microsoft.Extensions.Logging;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Notifications.Application.Commands.CreateNotification;

namespace ResX.Notifications.Application.IntegrationEvents.TransactionCreated;

public class TransactionCreatedNotificationHandler : IIntegrationEventHandler<TransactionCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionCreatedNotificationHandler> _logger;

    public TransactionCreatedNotificationHandler(
        IMediator mediator,
        ILogger<TransactionCreatedNotificationHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(
        TransactionCreatedIntegrationEvent transactionCreatedIntegrationEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating notification for transaction {TransactionId}",
            transactionCreatedIntegrationEvent.TransactionId);

        await _mediator.Send(new CreateNotificationCommand(
                transactionCreatedIntegrationEvent.DonorId,
                Domain.Enums.NotificationType.TransactionCreated,
                "New Transaction Request",
                $"Someone wants to receive your listing item.",
                new { transactionId = transactionCreatedIntegrationEvent.TransactionId }),
            cancellationToken);
    }
}