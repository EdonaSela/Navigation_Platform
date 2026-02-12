using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Journeys.Queries
{
    public record JourneyFavoriteDto(Guid Id, Guid JourneyId, string UserId);
    public record JourneyDto(
      Guid Id,
      string userId,
      string StartLocation,
      DateTime StartTime,
      string ArrivalLocation,
      DateTime ArrivalTime,
      string TransportType,
      decimal DistanceKm,
      bool IsDailyGoalAchieved,bool IsPublicLinkRevoked, string? PublicSharingToken, List<JourneyFavoriteDto> Favorites); 
}
