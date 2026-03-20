using MediatR;
using Microsoft.Extensions.Logging;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Users.Application.Commands.CreateUserProfile;

namespace ResX.Users.Application.IntegrationEvents.UserRegistered;

public class UserRegisteredIntegrationEventHandler : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        IMediator mediator,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredIntegrationEvent messageSentIntegrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling UserRegisteredIntegrationEvent for user {UserId}.",
            messageSentIntegrationEvent.UserId);

        await _mediator.Send(
            new CreateUserProfileCommand(
                messageSentIntegrationEvent.UserId,
                messageSentIntegrationEvent.FirstName,
                messageSentIntegrationEvent.LastName,   
                City: null),
            cancellationToken);
    }
}
