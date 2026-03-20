using ResX.EventBus.RabbitMQ.Events;

namespace ResX.EventBus.RabbitMQ.Abstractions;

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T messageSentIntegrationEvent, CancellationToken cancellationToken = default);
}
