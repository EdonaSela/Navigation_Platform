namespace JourneyService.Domain.Entities;

public class JourneyShare
{
    public Guid JourneyId { get; set; }
    public string SharedWithUserId { get; set; } = string.Empty;
    public Journey Journey { get; set; } = null!;
}
