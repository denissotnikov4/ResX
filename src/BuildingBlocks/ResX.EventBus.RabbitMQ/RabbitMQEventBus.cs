using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.EventBus.RabbitMQ.Events;
using System.Text;
using System.Text.Json;

namespace ResX.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQConnection _connection;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();
    private IChannel? _consumerChannel;
    private bool _disposed;

    public RabbitMQEventBus(
        RabbitMQConnection connection,
        ILogger<RabbitMQEventBus> logger,
        IServiceProvider serviceProvider,
        IOptions<EventBusOptions> options)
    {
        _connection = connection;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _exchangeName = options.Value.ExchangeName;
        _queueName = options.Value.QueueName;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        var eventName = integrationEvent.EventType;
        _logger.LogInformation("Publishing integration event: {EventName}", eventName);

        var connection = _connection.GetConnection();
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType());
        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: eventName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;

        if (!_handlers.ContainsKey(eventName))
            _handlers[eventName] = [];

        if (_handlers[eventName].Any(h => h == typeof(TH)))
        {
            _logger.LogWarning("Handler {HandlerType} already registered for {EventName}", typeof(TH).Name, eventName);
            return;
        }

        _handlers[eventName].Add(typeof(TH));
        _eventTypes[eventName] = typeof(T);

        _ = StartConsumingAsync(eventName);
    }

    private async Task StartConsumingAsync(string eventName)
    {
        if (_consumerChannel == null)
        {
            var connection = _connection.GetConnection();
            _consumerChannel = await connection.CreateChannelAsync();

            await _consumerChannel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Direct,
                durable: true);

            await _consumerChannel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        await _consumerChannel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: eventName);

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.ReceivedAsync += ConsumerReceivedAsync;

        await _consumerChannel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ConsumerReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var eventName = args.RoutingKey;
        var message = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            await ProcessEventAsync(eventName, message);
            await _consumerChannel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for event {EventName}", eventName);
            await _consumerChannel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ProcessEventAsync(string eventName, string message)
    {
        if (!_handlers.TryGetValue(eventName, out var handlerTypes)) return;
        if (!_eventTypes.TryGetValue(eventName, out var eventType)) return;

        var integrationEvent = JsonSerializer.Deserialize(message, eventType);
        if (integrationEvent == null) return;

        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in handlerTypes)
        {
            var handler = scope.ServiceProvider.GetRequiredService(handlerType);
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod != null)
                await (Task)handleMethod.Invoke(handler, [integrationEvent, CancellationToken.None])!;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _consumerChannel?.Dispose();
    }
}
