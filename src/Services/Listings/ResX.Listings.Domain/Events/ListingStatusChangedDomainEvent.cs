using ResX.Common.Domain;
using ResX.Listings.Domain.Enums;

namespace ResX.Listings.Domain.Events;

public record ListingStatusChangedDomainEvent(
    Guid ListingId,
    ListingStatus PreviousStatus,
    ListingStatus NewStatus) : DomainEvent;