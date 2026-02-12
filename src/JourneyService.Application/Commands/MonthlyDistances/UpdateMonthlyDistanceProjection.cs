using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.MonthlyDistances
{
    public class UpdateMonthlyDistanceProjection : INotificationHandler<JourneyCreatedEvent>
    {
        private readonly IApplicationDbContext _context;

        public UpdateMonthlyDistanceProjection(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(JourneyCreatedEvent notification, CancellationToken cancellationToken)
        {
                            var stats = await _context.MonthlyUserDistances
                    .FirstOrDefaultAsync(x =>
                        x.UserId == notification.UserId &&
                        x.Year == notification.Date.Year &&
                        x.Month == notification.Date.Month,
                        cancellationToken);

            // 3. Create or Update the record
            if (stats == null)
            {
                stats = new MonthlyUserDistance
                {
                    Id = Guid.NewGuid(),
                    UserId = notification.UserId, //
                    Year = notification.Date.Year, //
                    Month = notification.Date.Month, //
                    TotalDistanceKm = 0 // 0 para shtimit te distances se re
                };

                _context.MonthlyUserDistances.Add(stats);
            }

            // Add the distance from the new journey to the monthly total
            stats.TotalDistanceKm += notification.Distance.Value; 

            await _context.SaveChangesAsync(cancellationToken); 
        }

   
    }
}
