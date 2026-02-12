namespace JourneyService.Infrastructure.Messaging;

public interface IEventBusPublisher
{
    Task PublishAsync(OutboxEventEnvelope message, CancellationToken cancellationToken);
}

