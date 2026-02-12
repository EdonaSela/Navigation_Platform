using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Entities
{
    public class UserStatusAudit
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public UserStatus OldStatus { get; set; }
        public UserStatus NewStatus { get; set; }
        public string ChangedByAdminId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
