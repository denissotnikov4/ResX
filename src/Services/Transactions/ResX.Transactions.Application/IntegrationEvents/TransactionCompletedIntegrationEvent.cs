using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Transactions.Application.IntegrationEvents;

public record TransactionCompletedIntegrationEvent(
    Guid TransactionId,
    Guid DonorId,
    Guid RecipientId) : IntegrationEvent;