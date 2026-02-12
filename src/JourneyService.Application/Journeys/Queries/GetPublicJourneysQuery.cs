using JourneyService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Application.Journeys.Queries;

public record GetPublicJourneysQuery(int Page, int PageSize) : IRequest<List<JourneyDto>>;

public class GetPublicJourneysHandler : IRequestHandler<GetPublicJourneysQuery, List<JourneyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPublicJourneysHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JourneyDto>> Handle(GetPublicJourneysQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return await _context.Journeys
            .Where(j => !j.IsPublicLinkRevoked && j.PublicSharingToken != null)
            .OrderByDescending(j => j.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JourneyDto(
                j.Id,
                j.UserId,
                j.StartLocation,
                j.StartTime,
                j.ArrivalLocation,
                j.ArrivalTime,
                j.TransportType.ToString(),
                j.Distance.Value,
                j.IsDailyGoalAchieved,
                j.IsPublicLinkRevoked,
                j.PublicSharingToken,
                j.Favorites.Select(f => new JourneyFavoriteDto(f.Id, f.JourneyId, f.UserId)).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}
