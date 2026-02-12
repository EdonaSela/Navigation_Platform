namespace JourneyService.Domain.ValueObjects;

public record DistanceKm
{
    public decimal Value { get; init; }

    public DistanceKm(decimal value)
    {
        
        if (value < 0) throw new ArgumentException("Distance cannot be negative.");
        Value = value;
    }
}