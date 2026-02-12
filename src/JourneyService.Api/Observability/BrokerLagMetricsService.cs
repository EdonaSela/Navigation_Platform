using Microsoft.Extensions.Options;
using Prometheus;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JourneyService.Api.Observability;

public sealed class BrokerLagMetricsService : BackgroundService
{
    private static readonly Gauge BrokerLag = Metrics.CreateGauge(
        "navigation_broker_queue_lag_messages",
        "Current RabbitMQ queue lag in messages.",
        new GaugeConfiguration { LabelNames = new[] { "queue" } });

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrokerLagMetricsService> _logger;
    private readonly IConfiguration _configuration;

    public BrokerLagMetricsService(
        IHttpClientFactory httpClientFactory,
        ILogger<BrokerLagMetricsService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CollectLagAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect RabbitMQ lag metric.");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task CollectLagAsync(CancellationToken cancellationToken)
    {
        var host = _configuration["MessageBroker:Host"] ?? "localhost";
        var queue = _configuration["MessageBroker:RewardQueue"] ?? "reward-worker.journey-events";
        var user = _configuration["MessageBroker:Username"] ?? "guest";
        var pass = _configuration["MessageBroker:Password"] ?? "guest";
        var managementPort = _configuration["MessageBroker:ManagementPort"] ?? "15672";

        var base64Creds = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));
        var url = $"http://{host}:{managementPort}/api/queues/%2F/{Uri.EscapeDataString(queue)}";

        var client = _httpClientFactory.CreateClient("rabbitmq-metrics");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Creds);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        var messages = root.TryGetProperty("messages", out var m) ? m.GetInt32() : 0;
        BrokerLag.WithLabels(queue).Set(messages);
    }
}
