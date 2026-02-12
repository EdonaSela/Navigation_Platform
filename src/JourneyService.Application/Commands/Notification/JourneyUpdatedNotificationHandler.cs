using AutoMapper;
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.Notification;

public class JourneyUpdatedHandler : INotificationHandler<JourneyUpdatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IHubNotifier _hubNotification;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public JourneyUpdatedHandler(
        IApplicationDbContext context,
        IHubNotifier hubNotification,
        IEmailService emailService,
        IMapper mapper)
    {
        _context = context;
        _hubNotification = hubNotification;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task Handle(JourneyUpdatedEvent notification, CancellationToken ct)
    {
        var journey = await _context.Journeys
            .Include(j => j.Favorites)
            .FirstOrDefaultAsync(j => j.Id == notification.Id, ct);

        if (journey == null || journey.Favorites == null || !journey.Favorites.Any())
            return;

        var dto = _mapper.Map<JourneyDto>(journey);

        var notifyUserIds = journey.Favorites.Select(f => f.UserId).ToList();
        if (!notifyUserIds.Contains(journey.UserId))
        {
            notifyUserIds.Add(journey.UserId);
        }

        var onlineUsers = notifyUserIds
    .Where(userId => _hubNotification.IsUserOnline(userId)).ToList();

        var offlineUsers = notifyUserIds
            .Where(userId => !_hubNotification.IsUserOnline(userId)).ToList();

        if (onlineUsers.Any())
        {
            await _hubNotification.SendJourneyUpdatedToUsers(onlineUsers, dto);
        }

        await _hubNotification.SendJourneyUpdatedToAdmins(dto);

        foreach (var userId in offlineUsers)
        {
            
            //await _emailService.SendEmailAsync(userId, "Journey Updated",
            //    $"A journey you own or favorited ({journey.StartLocation}) has a new activity.");
            await _emailService.SendEmailAsync("edonasela@gmail.com", "Journey Updated",
             $"A journey you own or favorited ({journey.StartLocation}) has a new activity.");
        }
    }
}
