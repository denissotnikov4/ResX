using ResX.Common.Domain;

namespace ResX.Identity.Domain.Events;

public record UserRegisteredDomainEvent(
    Guid UserId,
    string Email) : DomainEvent;
