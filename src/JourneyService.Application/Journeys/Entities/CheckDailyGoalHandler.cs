using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static JourneyService.Domain.Events.JourneyEvents;

public class CheckDailyGoalHandler :
    INotificationHandler<JourneyCreatedEvent>,
    INotificationHandler<JourneyUpdatedEvent>,
    INotificationHandler<JourneyDeletedEvent>
{
    private readonly IApplicationDbContext _context;
   // private readonly IHubContext<Hub> _hubContext; 

    public CheckDailyGoalHandler(
        IApplicationDbContext context) 
    {
        _context = context;
        //_hubContext = hubContext;
    }

    //public CheckDailyGoalHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(JourneyCreatedEvent n, CancellationToken ct) => await EvaluateDailyGoal(n.UserId, n.Date, ct);
    public async Task Handle(JourneyUpdatedEvent n, CancellationToken ct) => await EvaluateDailyGoal(n.UserId, n.Date, ct);
    public async Task Handle(JourneyDeletedEvent n, CancellationToken ct) => await EvaluateDailyGoal(n.UserId, n.Date, ct);

    private async Task EvaluateDailyGoal(string userId, DateTime date, CancellationToken ct)
    {
        var journeysToday = await _context.Journeys
            .Where(j => j.UserId == userId && j.StartTime.Date == date.Date)
            .ToListAsync(ct);

        var totalDistance = journeysToday.Sum(j => j.Distance.Value);

        if (totalDistance >= 20.00m) 
        {
            if (!journeysToday.Any(j => j.IsDailyGoalAchieved))
            {
                var firstJourney = journeysToday.OrderBy(j => j.StartTime).First();
                firstJourney.MarkAsGoalAchiever();
                await _context.SaveChangesAsync(ct);
               // await _hubContext.Clients.All.SendAsync("JourneyUpdated", firstJourney, ct);

            }
        }
        else
        {
           
            var journeysWithBadges = journeysToday.Where(j => j.IsDailyGoalAchieved).ToList();
            if (journeysWithBadges.Any())
            {
                foreach (var j in journeysWithBadges)
                {
                    j.ResetGoal(); 
                }
                await _context.SaveChangesAsync(ct);
               // await _hubContext.Clients.All.SendAsync("JourneyUpdated", journeysWithBadges, ct);
            }
        }

        //var latestJourney = journeysToday.OrderByDescending(j => j.StartTime).FirstOrDefault();
        //if (latestJourney != null)
        //{
        //    await _hubContext.Clients.All.SendAsync("JourneyCreated", latestJourney, ct);
        //}
    }
}