using JourneyService.Application.Journeys.Queries;
using JourneyService.Domain.Entities;

public interface IHubNotifier
{
    Task SendJourneyCreated(Journey journey);
    Task SendJourneyDeleted(Guid id);
    Task SendJourneyUpdated(Journey journey);

    Task SendJourneyUpdatedToUsers(IEnumerable<string> userIds, JourneyDto journey);
    Task SendJourneyUpdatedToAdmins(JourneyDto journey);
    Task SendJourneyDeletedToUsers(IEnumerable<string> userIds, Guid journeyId);
    bool IsUserOnline(string userId);
}
