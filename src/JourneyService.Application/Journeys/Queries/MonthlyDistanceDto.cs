using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Journeys.Queries
{
    public class MonthlyDistanceDto
    {
      

        public string UserId { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalDistanceKm { get; set; }
    }
}
