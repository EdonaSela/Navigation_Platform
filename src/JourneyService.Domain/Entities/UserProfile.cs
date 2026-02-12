using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Domain.Entities
{
    public enum UserStatus { Active, Suspended, Deactivated }

    public class UserProfile
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;

       
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
