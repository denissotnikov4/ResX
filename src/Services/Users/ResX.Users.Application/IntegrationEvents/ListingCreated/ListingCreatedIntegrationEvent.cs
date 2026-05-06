using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Users.Application.IntegrationEvents.ListingCreated;

/// <summary>
/// Mirror of the contract published by the Listings service when a listing is created.
/// Carries the precomputed eco impact (weight × category rate) so Users service can
/// increment the donor's lifetime EcoStats without a cross-service lookup.
/// </summary>
public record ListingCreatedIntegrationEvent(
    Guid ListingId,
    Guid DonorId,
    string Title,
    string CategoryName,
    string City,
    int WeightGrams,
    int Co2SavedG,
    int WasteSavedG) : IntegrationEvent;
