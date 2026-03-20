namespace ResX.EventBus.RabbitMQ;

public class EventBusOptions
{
    public const string SectionName = "EventBus";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "resx_event_bus";
    public int RetryCount { get; set; } = 5;
    public string QueueName { get; set; } = string.Empty;
}
