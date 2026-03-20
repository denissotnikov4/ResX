using ResX.Common.Domain;

namespace ResX.Listings.Domain.Events;

public record ListingViewedDomainEvent(
    Guid ListingId,
    Guid DonorId) : DomainEvent;