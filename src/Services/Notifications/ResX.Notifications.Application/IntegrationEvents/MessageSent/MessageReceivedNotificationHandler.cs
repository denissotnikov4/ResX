using MediatR;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Notifications.Application.Commands.CreateNotification;

namespace ResX.Notifications.Application.IntegrationEvents.MessageSent;

public class MessageReceivedNotificationHandler : IIntegrationEventHandler<MessageSentIntegrationEvent>
{
    private readonly IMediator _mediator;

    public MessageReceivedNotificationHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task HandleAsync(MessageSentIntegrationEvent messageSentIntegrationEvent, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CreateNotificationCommand(
                messageSentIntegrationEvent.RecipientId,
                Domain.Enums.NotificationType.MessageReceived,
                "New Message",
                $"You have received a new message.",
                new
                {
                    conversationId = messageSentIntegrationEvent.ConversationId,
                    senderId = messageSentIntegrationEvent.SenderId
                }),
            cancellationToken);
    }
}
