using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Entities
{
    public class JourneyFavorite
    {
        public Guid Id { get; set; }
        public Guid JourneyId { get; set; }
        public string UserId { get; set; } = null!;

        
        public Journey Journey { get; set; } = null!;
    }
}
