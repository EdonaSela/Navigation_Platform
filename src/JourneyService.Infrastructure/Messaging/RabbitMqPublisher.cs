using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace JourneyService.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IEventBusPublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _sync = new();

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _exchangeName = configuration["MessageBroker:Exchange"] ?? "journey.events";

        _factory = new ConnectionFactory
        {
            HostName = configuration["MessageBroker:Host"] ?? "localhost",
            UserName = configuration["MessageBroker:Username"] ?? "guest",
            Password = configuration["MessageBroker:Password"] ?? "guest",
            VirtualHost = configuration["MessageBroker:VirtualHost"] ?? "/",
            Port = int.TryParse(configuration["MessageBroker:Port"], out var port) ? port : AmqpTcpEndpoint.UseDefaultPort,
            DispatchConsumersAsync = true
        };
    }

    public Task PublishAsync(OutboxEventEnvelope message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = message.MessageId.ToString();
        properties.Type = message.Type;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredOnUtc).ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: message.Type,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published outbox event {EventType} ({MessageId}) to RabbitMQ.", message.Type, message.MessageId);
        return Task.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        lock (_sync)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return;
            }

            _channel?.Dispose();
            _connection?.Dispose();

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

