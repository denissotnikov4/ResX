using MediatR;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Notifications.Application.Commands.CreateNotification;

namespace ResX.Notifications.Application.IntegrationEvents.TransactionCompleted;

public class TransactionCompletedNotificationHandler : IIntegrationEventHandler<TransactionCompletedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public TransactionCompletedNotificationHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task HandleAsync(
        TransactionCompletedIntegrationEvent transactionCompletedIntegrationEvent,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CreateNotificationCommand(
                transactionCompletedIntegrationEvent.DonorId,
                Domain.Enums.NotificationType.TransactionCompleted,
                "Transaction Completed",
                "The recipient has confirmed receipt of your item. Thank you for your contribution!",
                new { transactionId = transactionCompletedIntegrationEvent.TransactionId }),
            cancellationToken);

        await _mediator.Send(new CreateNotificationCommand(
                transactionCompletedIntegrationEvent.RecipientId,
                Domain.Enums.NotificationType.TransactionCompleted,
                "Item Received Successfully",
                "Your transaction has been marked as complete.",
                new { transactionId = transactionCompletedIntegrationEvent.TransactionId }), 
            cancellationToken);
    }
}