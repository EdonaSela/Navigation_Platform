using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JourneyService.Application.Journeys.Queries
{
    public class GetMonthlyStatsQueryHandler : IRequestHandler<GetMonthlyStatsQuery, PagedList<MonthlyDistanceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetMonthlyStatsQueryHandler> _logger;

        public GetMonthlyStatsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetMonthlyStatsQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<PagedList<MonthlyDistanceDto>> Handle(GetMonthlyStatsQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Handling monthly stats query. Page: {Page}, PageSize: {PageSize}, OrderBy: {OrderBy}", request.Page, request.PageSize, request.OrderBy);

            var query = _context.MonthlyUserDistances.AsNoTracking();

            var projection = query.Select(x => new MonthlyDistanceDto
            {
                UserId = x.UserId,
                Year = x.Year,
                Month = x.Month,
                TotalDistanceKm = x.TotalDistanceKm
            });

            projection = request.OrderBy switch
            {
                "TotalDistanceKm" => projection.OrderByDescending(x => x.TotalDistanceKm),
                "UserId" => projection.OrderBy(x => x.UserId),
                _ => projection.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            };

            var result = await PagedList<MonthlyDistanceDto>.CreateAsync(projection, request.Page, request.PageSize);
            _logger.LogInformation("Monthly stats query completed. ReturnedItems: {ReturnedItems}", result.Items.Count);

            return result;
        }
    }
}
