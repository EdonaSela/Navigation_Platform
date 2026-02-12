using JourneyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {

        DbSet<JourneyService.Domain.Entities.Journey> Journeys { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<JourneyFavorite> JourneyFavorites { get; }
        DbSet<JourneyShare> JourneyShares { get; }

        DbSet<MonthlyUserDistance> MonthlyUserDistances { get;  }
        public DbSet<UserProfile> Users { get; set; }
        public DbSet<UserStatusAudit> UserStatusAudits { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
