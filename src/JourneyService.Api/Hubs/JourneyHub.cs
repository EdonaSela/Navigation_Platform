using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace JourneyService.Api.Hubs;

[Authorize]
public class JourneyHub : Hub
{
    public const string AdminsGroup = "admins";
    public static readonly ConcurrentDictionary<string, byte> OnlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.TryAdd(userId, 0);
        }

        var isAdmin =
            Context.User?.IsInRole("Admin") == true ||
            Context.User?.Claims.Any(c =>
                c.Type == "roles" &&
                string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)) == true ||
            Context.User?.Claims.Any(c =>
                (c.Type == "scope" || c.Type == "scp") &&
                c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(scope => string.Equals(scope, "admin", StringComparison.OrdinalIgnoreCase))) == true;

        if (isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.TryRemove(userId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToJourney(string journeyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, journeyId);
    }
}
