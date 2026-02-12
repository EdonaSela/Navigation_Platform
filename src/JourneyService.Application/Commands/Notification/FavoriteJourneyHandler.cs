using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.Notification
{
    public class FavoriteJourneyHandler : IRequestHandler<FavoriteJourneyCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPublisher _publisher;

        public FavoriteJourneyHandler(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IPublisher publisher)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _publisher = publisher;
        }

        public async Task Handle(FavoriteJourneyCommand request, CancellationToken ct)
        {


            var userId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
                  ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? throw new UnauthorizedAccessException();


            var journey = await _context.Journeys
              .Include(j => j.Favorites)
              .AsNoTracking() 
              .FirstOrDefaultAsync(j => j.Id == request.JourneyId, ct);

            if (journey == null) return;

            if (!journey.Favorites.Any(f => f.UserId == userId))
            {
                var newFavorite = new JourneyFavorite
                {
                    //Id = Guid.NewGuid(),
                    UserId = userId,
                    JourneyId = journey.Id
                };

               
                _context.JourneyFavorites.Add(newFavorite);

                await _context.SaveChangesAsync(ct);
                await _publisher.Publish(new JourneyUpdatedEvent(journey.Id, DateTime.UtcNow, userId, journey.Distance, journey.StartTime, journey.Distance, journey.StartTime), ct);

            }
        }
    }
}
