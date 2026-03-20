using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Transactions.Application.IntegrationEvents;

public record TransactionCreatedIntegrationEvent(
    Guid TransactionId,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId) : IntegrationEvent;