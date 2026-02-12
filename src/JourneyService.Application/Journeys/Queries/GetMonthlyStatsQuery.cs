using JourneyService.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Journeys.Queries
{
    public record GetMonthlyStatsQuery : IRequest<PagedList<MonthlyDistanceDto>> //request
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? OrderBy { get; init; }
    }

}
