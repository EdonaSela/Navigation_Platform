using System.Text;
using System.Text.Json;
using JourneyService.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RewardWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;

    private readonly string _exchangeName;
    private readonly string _queueName;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _exchangeName = _configuration["MessageBroker:Exchange"] ?? "journey.events";
        _queueName = _configuration["MessageBroker:RewardQueue"] ?? "reward-worker.journey-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["MessageBroker:Host"] ?? "localhost",
            UserName = _configuration["MessageBroker:Username"] ?? "guest",
            Password = _configuration["MessageBroker:Password"] ?? "guest",
            VirtualHost = _configuration["MessageBroker:VirtualHost"] ?? "/",
            Port = int.TryParse(_configuration["MessageBroker:Port"], out var port) ? port : AmqpTcpEndpoint.UseDefaultPort,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        _channel.QueueBind(_queueName, _exchangeName, "JourneyCreatedEvent");
        _channel.QueueBind(_queueName, _exchangeName, "JourneyUpdatedEvent");
        _channel.QueueBind(_queueName, _exchangeName, "JourneyDeletedEvent");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnReceivedAsync;

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RewardWorker consuming RabbitMQ queue {Queue} on exchange {Exchange}.", _queueName, _exchangeName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var envelope = JsonSerializer.Deserialize<OutboxEventEnvelope>(body);

            if (envelope is null || string.IsNullOrWhiteSpace(envelope.Type))
            {
                _logger.LogWarning("Received invalid RabbitMQ message body. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var evaluator = scope.ServiceProvider.GetRequiredService<DailyGoalEvaluator>();
            await evaluator.ProcessAsync(envelope);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "RewardWorker received malformed JSON and will drop message. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RewardWorker failed to process message. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
