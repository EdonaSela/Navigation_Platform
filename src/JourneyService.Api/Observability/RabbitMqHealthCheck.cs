using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace JourneyService.Api.Observability;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["MessageBroker:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["MessageBroker:Port"], out var parsedPort) ? parsedPort : 5672;

        using var tcpClient = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            await tcpClient.ConnectAsync(host, port, timeoutCts.Token);
            return HealthCheckResult.Healthy("RabbitMQ is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is not reachable.", ex);
        }
    }
}
