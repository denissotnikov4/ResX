using ResX.Common.Domain;

namespace ResX.Listings.Domain.Events;

public record ListingCreatedDomainEvent(
    Guid ListingId,
    Guid DonorId,
    string CategoryName) : DomainEvent;