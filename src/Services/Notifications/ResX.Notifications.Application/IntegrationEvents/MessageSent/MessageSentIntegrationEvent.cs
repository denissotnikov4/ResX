using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Notifications.Application.IntegrationEvents.MessageSent;

public record MessageSentIntegrationEvent(
    Guid ConversationId,
    Guid SenderId,
    Guid RecipientId,
    string Content,
    DateTime SentAt) : IntegrationEvent;