using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Listings.Application.IntegrationEvents;

public record ListingCreatedIntegrationEvent(
    Guid ListingId,
    Guid DonorId,
    string Title,
    string CategoryName,
    string City) : IntegrationEvent;

public record ListingStatusChangedIntegrationEvent(
    Guid ListingId,
    Guid DonorId,
    string PreviousStatus,
    string NewStatus) : IntegrationEvent;
