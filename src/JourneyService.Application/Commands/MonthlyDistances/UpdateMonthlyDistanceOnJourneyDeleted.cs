using JourneyService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.MonthlyDistances
{
    public class UpdateMonthlyDistanceOnJourneyDeleted : INotificationHandler<JourneyDeletedEvent>
    {
        private readonly IApplicationDbContext _context;

        public UpdateMonthlyDistanceOnJourneyDeleted(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(JourneyDeletedEvent notification, CancellationToken ct)
        {
            var stats = await _context.MonthlyUserDistances
        .FirstOrDefaultAsync(x =>
            x.UserId == notification.UserId &&
            x.Year == notification.Date.Year &&
            x.Month == notification.Date.Month,
            ct);

            if (stats != null)
            {
                stats.TotalDistanceKm -= notification.Distance.Value;
                if (stats.TotalDistanceKm <= 0)
                {
                    _context.MonthlyUserDistances.Remove(stats);
                }

                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
