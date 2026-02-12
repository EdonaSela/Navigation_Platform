namespace JourneyService.Infrastructure.Messaging;

public sealed class OutboxEventEnvelope
{
    public Guid MessageId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime OccurredOnUtc { get; init; }
}

