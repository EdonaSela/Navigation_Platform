using JourneyService.Api.Hubs;
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Queries;
using JourneyService.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class HubNotifier : IHubNotifier
{
    private readonly IHubContext<JourneyHub> _hubContext;
    public static readonly ConcurrentDictionary<string, byte> OnlineUsers = new();
    public HubNotifier(IHubContext<JourneyHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendJourneyCreated(Journey journey) =>
        _hubContext.Clients.All.SendAsync("JourneyCreated", journey);

    public Task SendJourneyDeleted(Guid id) =>
        _hubContext.Clients.All.SendAsync("JourneyDeleted", id);

    public Task SendJourneyUpdated(Journey journey) =>
        _hubContext.Clients.All.SendAsync("JourneyUpdated", journey);

    public async Task SendJourneyUpdatedToUsers(IEnumerable<string> userIds, JourneyDto journey) =>
         await _hubContext.Clients.Users(userIds.ToList()).SendAsync("JourneyUpdated", journey);

    public Task SendJourneyUpdatedToAdmins(JourneyDto journey) =>
        _hubContext.Clients.Group(JourneyHub.AdminsGroup).SendAsync("JourneyUpdated", journey);
   

    public async Task SendJourneyDeletedToUsers(IEnumerable<string> userIds, Guid journeyId)=>await _hubContext.Clients.Users(userIds.ToList())
            .SendAsync("JourneyDeleted", journeyId);

    public bool IsUserOnline(string userId)
    {
        return JourneyHub.OnlineUsers.ContainsKey(userId);
    }



}
