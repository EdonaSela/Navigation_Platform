using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetPagedJourneysQuery(int Page, int PageSize) : IRequest<List<JourneyDto>>;

public class GetPagedJourneysHandler : IRequestHandler<GetPagedJourneysQuery, List<JourneyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPagedJourneysHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<JourneyDto>> Handle(GetPagedJourneysQuery request, CancellationToken ct)
    {
        var query = _context.Journeys.AsQueryable();

        if (!_currentUserService.IsAdmin)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new List<JourneyDto>();
            }

            query = query.Where(j =>
                j.UserId == currentUserId ||
                j.SharedWithUsers.Any(s => s.SharedWithUserId == currentUserId));
        }

        return await query
            .OrderByDescending(x => x.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JourneyDto(
                j.Id,j.UserId, j.StartLocation, j.StartTime, j.ArrivalLocation,
                j.ArrivalTime, j.TransportType.ToString(), j.Distance.Value, j.IsDailyGoalAchieved,j.IsPublicLinkRevoked,j.PublicSharingToken, j.Favorites.Select(f => new JourneyFavoriteDto(f.Id, f.JourneyId, f.UserId)).ToList()))
            .ToListAsync(ct);
    }
}
