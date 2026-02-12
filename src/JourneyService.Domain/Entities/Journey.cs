namespace JourneyService.Domain.Entities;

using JourneyService.Domain.Enums;
using JourneyService.Domain.Events;
using JourneyService.Domain.ValueObjects;
using MediatR;
using static JourneyService.Domain.Events.JourneyEvents;

public class Journey
{
    public Guid Id { get; private set; }
    public string StartLocation { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public string ArrivalLocation { get; private set; } = string.Empty;
    public DateTime ArrivalTime { get; private set; }
    public TransportType TransportType { get; private set; }
    public DistanceKm Distance { get; private set; } = null!;
    public string UserId { get; private set; } = string.Empty;

    public string OwnerId { get; private set; }

    public string? PublicSharingToken { get; private set; }
    public bool IsPublicLinkRevoked { get; private set; }

    public List<JourneyShare> SharedWithUsers { get; private set; } = new();

   


    


    private readonly List<INotification> _domainEvents = new();
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(INotification domainEvent) => _domainEvents.Add(domainEvent);
   

    private Journey() { } 

    public static Journey Create(
        string userId,
        string ownerId,
        string start,
        DateTime startTime,
        string arrival,
        DateTime arrivalTime,
        TransportType type,
        decimal distanceValue)
    {
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OwnerId= ownerId,
            StartLocation = start,
            StartTime = startTime,
            ArrivalLocation = arrival,
            ArrivalTime = arrivalTime,
            TransportType = type,
            Distance = new DistanceKm(distanceValue) 
        };

       // journey._domainEvents.Add(new JourneyCreatedEvent(journey.Id, userId));
        journey.AddDomainEvent(new JourneyCreatedEvent(journey.Id,journey.StartTime, journey.UserId,journey.Distance));
        return journey;
    }
    public void Update(string start, DateTime startTime, string arrival, DateTime arrivalTime, TransportType type, decimal distance)
    {
        var oldDistance = this.Distance;
        var oldDate = this.StartTime;
        StartLocation = start;
        StartTime = startTime;
        ArrivalLocation = arrival;
        ArrivalTime = arrivalTime;
        TransportType = type;
        Distance = new DistanceKm(distance);

        _domainEvents.Add(new JourneyUpdatedEvent(this.Id, this.StartTime,this.UserId, oldDistance,
        oldDate, this.Distance, this.StartTime));
    }


    public void ClearDomainEvents() => _domainEvents.Clear();
    public bool IsDailyGoalAchieved { get; private set; } 

    public void MarkAsGoalAchiever()
    {
        IsDailyGoalAchieved = true;
        _domainEvents.Add(new DailyGoalAchievedEvent(this.UserId, this.StartTime.Date));
    }
    public void MarkAsDeleted()
    {
        AddDomainEvent(new JourneyDeletedEvent(this.Id, this.StartTime.Date,this.UserId, this.Distance ));
    }

    public void ResetGoal()
    {
        IsDailyGoalAchieved = false;
    }

    public void ShareWithUser(string userId)
    {
        if (!SharedWithUsers.Any(s => s.SharedWithUserId == userId))
        {
            SharedWithUsers.Add(new JourneyShare
            {
                JourneyId = Id,
                SharedWithUserId = userId
            });
        }
    }

    public void GeneratePublicLink()
    {
        PublicSharingToken = Guid.NewGuid().ToString("N");
        IsPublicLinkRevoked = false;
    }

    public void RevokePublicLink()
    {
        IsPublicLinkRevoked = true;
    }

    public List<JourneyFavorite> Favorites { get; private set; } = new();
    public void AddFavorite(string userId)
    {
        if (!Favorites.Any(f => f.UserId == userId))
        {
            Favorites.Add(new JourneyFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                JourneyId = this.Id
            });
        }
    }

    public void RemoveFavorite(string userId)
    {
        var existing = Favorites.FirstOrDefault(f => f.UserId == userId);
        if (existing != null)
        {
            Favorites.Remove(existing);
        }
    }
}
