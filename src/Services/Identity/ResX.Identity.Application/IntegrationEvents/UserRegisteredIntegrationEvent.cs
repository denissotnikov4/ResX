using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Identity.Application.IntegrationEvents;

public record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IntegrationEvent;
