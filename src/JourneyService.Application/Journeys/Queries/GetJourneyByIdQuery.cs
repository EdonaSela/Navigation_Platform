using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetJourneyByIdQuery(Guid Id) : IRequest<JourneyDto?>;

public class GetJourneyByIdHandler : IRequestHandler<GetJourneyByIdQuery, JourneyDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetJourneyByIdHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<JourneyDto?> Handle(GetJourneyByIdQuery request, CancellationToken ct)
    {
        var query = _context.Journeys.Where(x => x.Id == request.Id);

        if (!_currentUserService.IsAdmin)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            query = query.Where(j =>
                j.UserId == currentUserId ||
                j.SharedWithUsers.Any(s => s.SharedWithUserId == currentUserId));
        }

        return await query
            .Select(j => new JourneyDto(
                j.Id, j.UserId, j.StartLocation, j.StartTime, j.ArrivalLocation,
                j.ArrivalTime, j.TransportType.ToString(), j.Distance.Value, j.IsDailyGoalAchieved,j.IsPublicLinkRevoked,j.PublicSharingToken, j.Favorites.Select(f => new JourneyFavoriteDto(f.Id, f.JourneyId, f.UserId)).ToList()))
            .FirstOrDefaultAsync(ct);
    }
}
