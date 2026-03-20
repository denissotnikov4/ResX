using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Notifications.Application.IntegrationEvents.TransactionCreated;

public record TransactionCreatedIntegrationEvent(
    Guid TransactionId,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId) : IntegrationEvent;