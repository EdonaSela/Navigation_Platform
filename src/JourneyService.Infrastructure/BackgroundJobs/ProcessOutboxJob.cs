using JourneyService.Infrastructure.Messaging;
using JourneyService.Infrastructure.Persistence;
using JourneyService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ProcessOutboxJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBusPublisher _eventBusPublisher;
    private readonly ILogger<ProcessOutboxJob> _logger;

    public ProcessOutboxJob(
        IServiceProvider serviceProvider,
        IEventBusPublisher eventBusPublisher,
        ILogger<ProcessOutboxJob> logger)
    {
        _serviceProvider = serviceProvider;
        _eventBusPublisher = eventBusPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var messages = await db.Set<OutboxMessage>()
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    await _eventBusPublisher.PublishAsync(new OutboxEventEnvelope
                    {
                        MessageId = message.Id,
                        Type = message.Type,
                        Content = message.Content,
                        OccurredOnUtc = message.OccurredOnUtc
                    }, stoppingToken);

                    message.ProcessedOnUtc = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox worker crashed unexpectedly.");
            throw;
        }
    }
}
