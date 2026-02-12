using JourneyService.Application.Common.Interfaces;

using JourneyService.Domain.Entities;
using JourneyService.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Journey> Journeys { get; set; }
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<JourneyFavorite> JourneyFavorites { get; set; }
        public DbSet<JourneyShare> JourneyShares { get; set; }
        public DbSet<MonthlyUserDistance> MonthlyUserDistances { get; set; }
        public DbSet<UserProfile> Users { get; set; }
        public DbSet<UserStatusAudit> UserStatusAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JourneyFavorite>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.HasOne(f => f.Journey)
                      .WithMany(j => j.Favorites)
                      .HasForeignKey(f => f.JourneyId);

               
                entity.HasIndex(f => f.UserId);
            });

            modelBuilder.Entity<JourneyShare>(entity =>
            {
                entity.HasKey(s => new { s.JourneyId, s.SharedWithUserId });

                entity.HasOne(s => s.Journey)
                      .WithMany(j => j.SharedWithUsers)
                      .HasForeignKey(s => s.JourneyId);

                entity.HasIndex(s => s.SharedWithUserId);
            });
        

           

            modelBuilder.Entity<Journey>(entity =>
            {
                entity.Property(j => j.IsDailyGoalAchieved)
                .HasDefaultValue(false);

                entity.HasKey(j => j.Id);

                entity.OwnsOne(j => j.Distance, d =>
                {
                    d.Property(p => p.Value)
                     .HasColumnName("DistanceKm") 
                     .HasPrecision(5, 2);         
                });

                entity.Property(j => j.StartLocation).IsRequired().HasMaxLength(200);
                entity.Property(j => j.ArrivalLocation).IsRequired().HasMaxLength(200);

             
              
            });
            modelBuilder.Entity<UserProfile>().HasKey(u => u.Id);
            modelBuilder.Entity<Journey>()
                .HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(j => j.UserId);

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}
