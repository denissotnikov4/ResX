using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Transactions.Application.IntegrationEvents;

public record TransactionCompletedIntegrationEvent(
    Guid TransactionId,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId,
    int WeightGrams,
    int Co2SavedG,
    int WasteSavedG) : IntegrationEvent;