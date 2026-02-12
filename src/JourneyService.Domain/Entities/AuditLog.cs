using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Entities
{
    public class AuditLog 
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "Share", "Revoke", "GenerateLink"
        public Guid JourneyId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
