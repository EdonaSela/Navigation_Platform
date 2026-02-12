using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Journeys.Handlers;

public class DeleteJourneyCommandHandler : IRequestHandler<DeleteJourneyCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private readonly IHubNotifier _hubNotifier;
    private readonly IPublisher _publisher;
    public DeleteJourneyCommandHandler(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor,  IHubNotifier hubNotifier, IPublisher publisher)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _hubNotifier = hubNotifier;
        _publisher = publisher;
    }

    public async Task Handle(DeleteJourneyCommand request, CancellationToken ct)
    {

        var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
             ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var journey = await _context.Journeys
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (journey == null)
        {
            throw new KeyNotFoundException("Journey not found.");
        }

      
        if (journey.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this journey.");
        }
        journey.MarkAsDeleted();

        _context.Journeys.Remove(journey);

        await _context.SaveChangesAsync(ct);
        await _hubNotifier.SendJourneyDeleted(journey.Id);
        await _publisher.Publish(new JourneyDeletedEvent(journey.Id, journey.ArrivalTime, journey.UserId, journey.Distance), ct);

     

    }
}