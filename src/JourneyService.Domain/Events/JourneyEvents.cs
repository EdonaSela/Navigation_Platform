using JourneyService.Domain.Entities;
using JourneyService.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Events
{
    public class JourneyEvents
    {

      

        public record JourneyCreatedEvent(Guid Id,DateTime Date, string UserId, DistanceKm Distance) : INotification;
        public record DailyGoalAchievedEvent(string UserId, DateTime Date) : INotification;
        public record JourneyUpdatedEvent(Guid Id, DateTime Date, string UserId, DistanceKm OldDistance,DateTime OldDate, DistanceKm NewDistance, DateTime NewDate) : INotification;
        public record JourneyDeletedEvent(Guid Id, DateTime Date, string UserId, DistanceKm Distance) : INotification;

        public record UserStatusChangedEvent(string UserId, UserStatus NewStatus) : INotification;

    }
}
