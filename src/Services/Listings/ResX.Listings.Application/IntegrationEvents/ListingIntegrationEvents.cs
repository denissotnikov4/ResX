using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Listings.Application.IntegrationEvents;

public record ListingCreatedIntegrationEvent(
    Guid ListingId,
    Guid DonorId,
    string Title,
    string CategoryName,
    string City,
    int WeightGrams,
    int Co2SavedG,
    int WasteSavedG) : IntegrationEvent;

public record ListingStatusChangedIntegrationEvent(
    Guid ListingId,
    Guid DonorId,
    string PreviousStatus,
    string NewStatus) : IntegrationEvent;
