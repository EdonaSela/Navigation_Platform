using JourneyService.Application.Common.Interfaces; 
using JourneyService.Application.Journeys.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JourneyService.Application.Jorneys.Exceptions;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Journeys.Handlers;

public class UpdateJourneyCommandHandler : IRequestHandler<UpdateJourneyCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubNotifier _hubNotifier;
    private readonly IPublisher _publisher;

    public UpdateJourneyCommandHandler(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IHubNotifier hubNotifier, IPublisher publisher)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _hubNotifier = hubNotifier;
        _publisher = publisher;
    }

    public async Task Handle(UpdateJourneyCommand request, CancellationToken ct)
    {
      
        //var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);


        var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
             ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var journey = await _context.Journeys.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (journey == null) throw new KeyNotFoundException("Journey not found");

  
        if (journey.UserId != currentUserId)
        {
            throw new ForbiddenAccessException();
        }

        var oldDistance = journey.Distance;
        var oldDate = journey.StartTime;

        journey.Update(
            request.StartLocation,
            request.StartTime,
            request.ArrivalLocation,
            request.ArrivalTime,
            request.TransportType,
            request.DistanceKm);
        

        await _context.SaveChangesAsync(ct);


        //await _publisher.Publish(new JourneyUpdatedEvent(journey.Id, DateTime.UtcNow, currentUserId), ct);
        await _publisher.Publish(new JourneyUpdatedEvent(
                    journey.Id,
                     DateTime.UtcNow,
                    currentUserId,
                    oldDistance,
                    oldDate,
                    journey.Distance,
                    journey.StartTime
                ), ct);


    }
}
