using ResX.Common.Domain;

namespace ResX.Identity.Domain.Events;

public record UserLoggedInDomainEvent(
    Guid UserId,
    string Email) : DomainEvent;
