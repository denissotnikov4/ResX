using ResX.EventBus.RabbitMQ.Events;

namespace ResX.Users.Application.IntegrationEvents.UserRegistered;

public record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IntegrationEvent;