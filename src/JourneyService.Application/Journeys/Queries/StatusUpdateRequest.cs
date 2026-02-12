using JourneyService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Journeys.Queries
{
    public class StatusUpdateRequest
    {
      
        public UserStatus Status { get; set; }
    }
}
