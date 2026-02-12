
using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

using System.Security.Claims;
using static JourneyService.Domain.Events.JourneyEvents;




namespace JourneyService.Application.Journeys.Commands;

public class CreateJourneyCommandHandler : IRequestHandler<CreateJourneyCommand, Guid>
{
    private readonly IApplicationDbContext _context; 
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubNotifier _hubNotifier;
    private readonly IPublisher _publisher;

    public CreateJourneyCommandHandler(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IHubNotifier hubNotifier,IPublisher publisher)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _hubNotifier = hubNotifier;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateJourneyCommand request, CancellationToken cancellationToken)
    {
        //var userId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        //             ?? throw new UnauthorizedAccessException();

        //var userId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
        //     ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        //     ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
             ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? throw new UnauthorizedAccessException();

        var journey = JourneyService.Domain.Entities.Journey.Create(
            userId,
            userId,
            request.StartLocation,
            request.StartTime,
            request.ArrivalLocation,
            request.ArrivalTime,
            request.TransportType,
            request.DistanceKm);

        _context.Journeys.Add(journey);
        await _context.SaveChangesAsync(cancellationToken);

        // await _hubContext.Clients.All.SendAsync("JourneyCreated", journey);

        await _hubNotifier.SendJourneyCreated(journey);

        
        await _publisher.Publish(new JourneyCreatedEvent(journey.Id, DateTime.UtcNow, userId, journey.Distance), cancellationToken);
        return journey.Id;
    }
}