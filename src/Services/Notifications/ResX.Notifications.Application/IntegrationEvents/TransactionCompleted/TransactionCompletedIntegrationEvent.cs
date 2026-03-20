using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Notifications.Application.IntegrationEvents.TransactionCompleted;

public record TransactionCompletedIntegrationEvent(
    Guid TransactionId,
    Guid DonorId,
    Guid RecipientId) : IntegrationEvent;
