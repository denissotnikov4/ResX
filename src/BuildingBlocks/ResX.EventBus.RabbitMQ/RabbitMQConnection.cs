using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using RabbitMQ.Client.Events;

namespace ResX.EventBus.RabbitMQ;

public class RabbitMQConnection : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQConnection> _logger;
    private readonly int _retryCount;
    private IConnection? _connection;
    private bool _disposed;
    private readonly Lock _syncRoot = new();

    public bool IsConnected => _connection?.IsOpen == true && !_disposed;

    public RabbitMQConnection(
        IOptions<EventBusOptions> options,
        ILogger<RabbitMQConnection> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _retryCount = opts.RetryCount;

        _connectionFactory = new ConnectionFactory
        {
            HostName = opts.HostName,
            Port = opts.Port,
            UserName = opts.UserName,
            Password = opts.Password,
            VirtualHost = opts.VirtualHost
        };
    }

    public IConnection GetConnection()
    {
        if (!IsConnected)
        {
            lock (_syncRoot)
            {
                if (!IsConnected)
                    TryConnect();
            }
        }
        return _connection!;
    }

    private void TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect...");

        var policy = Policy
            .Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(
                _retryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, time) => _logger.LogWarning(ex,
                    "RabbitMQ Client could not connect after {TimeOut}s", $"{time.TotalSeconds:n1}"));

        policy.Execute(() =>
        {
            _connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        if (IsConnected)
        {
            _connection!.ConnectionShutdownAsync += OnConnectionShutdown;
            _connection!.CallbackExceptionAsync += OnCallbackException;
            _connection!.ConnectionBlockedAsync += OnConnectionBlocked;
            _logger.LogInformation("RabbitMQ Client acquired a persistent connection.");
        }
        else
        {
            _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened.");
        }
    }

    private Task OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection is blocked. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection threw exception. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        _logger.LogWarning("RabbitMQ connection shutdown. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}
