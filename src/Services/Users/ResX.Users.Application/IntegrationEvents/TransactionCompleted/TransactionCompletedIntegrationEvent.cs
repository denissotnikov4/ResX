using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Users.Application.IntegrationEvents.TransactionCompleted;

/// <summary>
/// Mirror of the contract published by Transactions when a transaction reaches Completed.
/// Carries the precomputed eco impact so Users can update both donor's and recipient's
/// EcoStats without any cross-service lookups.
/// </summary>
public record TransactionCompletedIntegrationEvent(
    Guid TransactionId,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId,
    int WeightGrams,
    int Co2SavedG,
    int WasteSavedG) : IntegrationEvent;
