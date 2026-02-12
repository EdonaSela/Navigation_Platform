using JourneyService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JourneyService.Application.Journeys.Queries
{
    public class GetAdminJourneysHandler : IRequestHandler<GetAdminJourneysQuery, AdminJourneysResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetAdminJourneysHandler> _logger;

        public GetAdminJourneysHandler(IApplicationDbContext context, ILogger<GetAdminJourneysHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminJourneysResponse> Handle(GetAdminJourneysQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Handling admin journeys query. Page: {Page}, PageSize: {PageSize}, OrderBy: {OrderBy}, Direction: {Direction}",
                request.Page, request.PageSize, request.OrderBy, request.Direction);

            var query = _context.Journeys.AsQueryable();

            if (!string.IsNullOrEmpty(request.UserId))
                query = query.Where(j => j.UserId == request.UserId);

            if (!string.IsNullOrEmpty(request.TransportType))
                query = query.Where(j => j.TransportType.ToString() == request.TransportType);

            if (request.MinDistance.HasValue)
                query = query.Where(j => j.Distance.Value >= (decimal)request.MinDistance.Value);

            if (request.MaxDistance.HasValue)
                query = query.Where(j => j.Distance.Value <= (decimal)request.MaxDistance.Value);

            if (request.StartDateFrom.HasValue)
                query = query.Where(j => j.StartTime >= request.StartDateFrom.Value);

            if (request.StartDateTo.HasValue)
                query = query.Where(j => j.StartTime <= request.StartDateTo.Value);

            if (request.ArrivalDateFrom.HasValue)
                query = query.Where(j => j.ArrivalTime >= request.ArrivalDateFrom.Value);

            if (request.ArrivalDateTo.HasValue)
                query = query.Where(j => j.ArrivalTime <= request.ArrivalDateTo.Value);

            var total = await query.CountAsync(cancellationToken);

            var isDescending = string.Equals(request.Direction, "desc", StringComparison.OrdinalIgnoreCase);
            var orderBy = request.OrderBy?.Trim() ?? "StartTime";

            query = (orderBy.ToLowerInvariant(), isDescending) switch
            {
                ("arrivaltime", true) => query.OrderByDescending(j => j.ArrivalTime),
                ("arrivaltime", false) => query.OrderBy(j => j.ArrivalTime),
                ("distance", true) => query.OrderByDescending(j => j.Distance.Value),
                ("distance", false) => query.OrderBy(j => j.Distance.Value),
                ("transporttype", true) => query.OrderByDescending(j => j.TransportType),
                ("transporttype", false) => query.OrderBy(j => j.TransportType),
                ("starttime", false) => query.OrderBy(j => j.StartTime),
                _ => query.OrderByDescending(j => j.StartTime)
            };

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
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
                    j.Favorites.Select(f => new JourneyFavoriteDto(Guid.Empty, j.Id, f.UserId)).ToList()
                ))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Admin journeys query completed. TotalCount: {TotalCount}, ReturnedItems: {ReturnedItems}", total, items.Count);
            return new AdminJourneysResponse { Items = items, TotalCount = total };
        }
    }
}
