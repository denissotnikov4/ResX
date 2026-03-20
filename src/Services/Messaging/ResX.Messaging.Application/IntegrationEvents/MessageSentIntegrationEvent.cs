using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Messaging.Application.IntegrationEvents;

public record MessageSentIntegrationEvent(
    Guid ConversationId,
    Guid SenderId,
    Guid RecipientId,
    string Content,
    DateTime SentAt) : IntegrationEvent;