using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Journeys.Queries
{
    public class GetAdminJourneysQuery : IRequest<AdminJourneysResponse>
    {
        public string? UserId { get; set; }
        public string? TransportType { get; set; }
        public decimal? MinDistance { get; set; }
        public decimal? MaxDistance { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? ArrivalDateFrom { get; set; }
        public DateTime? ArrivalDateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "StartTime";
        public string Direction { get; set; } = "desc";
    }

    public class AdminJourneysResponse
    {
        public List<JourneyDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
