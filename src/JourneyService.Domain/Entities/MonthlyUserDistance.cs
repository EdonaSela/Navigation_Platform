using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Entities
{
    public class MonthlyUserDistance
    {
        public Guid Id { get; set; } 
        public string UserId { get; set; } 
        public int Year { get; set; } 
        public int Month { get; set; } 
        public decimal TotalDistanceKm { get; set; } 
    }
}
