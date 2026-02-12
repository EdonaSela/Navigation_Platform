using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
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
    public class UpdateMonthlyDistanceOnJourneyUpdated: INotificationHandler<JourneyUpdatedEvent>
    {
        private readonly IApplicationDbContext _context;

        public UpdateMonthlyDistanceOnJourneyUpdated(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(JourneyUpdatedEvent notification, CancellationToken ct)
        {
           //shikohet nese udhetimi eshte ne te njetin muaj/vit
            bool isSameMonth = notification.OldDate.Year == notification.NewDate.Year &&
                              notification.OldDate.Month == notification.NewDate.Month;

            if (isSameMonth)
            {
                var stats = await _context.MonthlyUserDistances
                    .FirstOrDefaultAsync(x =>
                        x.UserId == notification.UserId &&
                        x.Year == notification.NewDate.Year &&
                        x.Month == notification.NewDate.Month,
                        ct);

                if (stats != null)
                {
                    var difference = notification.NewDistance.Value - notification.OldDistance.Value;
                    stats.TotalDistanceKm += difference;

                    if (stats.TotalDistanceKm <= 0)
                    {
                        _context.MonthlyUserDistances.Remove(stats);
                    }
                }
            }
            else
            {
               //nese jane ne muaj te ndryshem hiqet nga muaji i vjeter shtohet tek i riu
                var oldStats = await _context.MonthlyUserDistances
                    .FirstOrDefaultAsync(x =>
                        x.UserId == notification.UserId &&
                        x.Year == notification.OldDate.Year &&
                        x.Month == notification.OldDate.Month,
                        ct);

                if (oldStats != null)
                {
                    oldStats.TotalDistanceKm -= notification.OldDistance.Value;
                    if (oldStats.TotalDistanceKm <= 0)
                    {
                        _context.MonthlyUserDistances.Remove(oldStats);
                    }
                }

               
                var newStats = await _context.MonthlyUserDistances
                    .FirstOrDefaultAsync(x =>
                        x.UserId == notification.UserId &&
                        x.Year == notification.NewDate.Year &&
                        x.Month == notification.NewDate.Month,
                        ct);

                if (newStats == null)
                {
                    newStats = new MonthlyUserDistance
                    {
                        UserId = notification.UserId,
                        Year = notification.NewDate.Year,
                        Month = notification.NewDate.Month,
                        TotalDistanceKm = 0
                    };
                    _context.MonthlyUserDistances.Add(newStats);
                }

                newStats.TotalDistanceKm += notification.NewDistance.Value;
            }

            
            await _context.SaveChangesAsync(ct);
        }
    }
}
