using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Transactions.Application.IntegrationEvents;

public record TransactionCancelledIntegrationEvent(Guid TransactionId) : IntegrationEvent;