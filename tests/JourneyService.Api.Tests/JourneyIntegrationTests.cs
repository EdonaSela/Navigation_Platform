using FluentAssertions;
using JourneyService.Domain.Entities;
using JourneyService.Domain.Enums;
using JourneyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JourneyService.Api.Tests
{

 
    public class JourneyIntegrationTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Journey_Creation_And_Persistence_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            // Act
            var journey = Journey.Create(
                userId: testUserId,
                ownerId: ownerId,
                start: "Central Park",
                startTime: DateTime.UtcNow,
                arrival: "Times Square",
                arrivalTime: DateTime.UtcNow.AddHours(2),
                type: TransportType.Bike,
                distanceValue: 25.5m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Assert - Journey persisted successfully
            var savedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            savedJourney.Should().NotBeNull();
            savedJourney!.Distance.Value.Should().Be(25.5m);
            savedJourney.UserId.Should().Be(testUserId);
            savedJourney.StartLocation.Should().Be("Central Park");
            savedJourney.ArrivalLocation.Should().Be("Times Square");
        }

        [Fact]
        public async Task Journey_With_Distance_Over_20km_And_Daily_Goal_Integration()
        {
            // Arrange - REQUIREMENT U-3: Daily distance reward badge
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            // Act - Create journey with 25.5km (exceeds 20km threshold)
            var journey = Journey.Create(
                userId: testUserId,
                ownerId: ownerId,
                start: "Gym",
                startTime: DateTime.UtcNow,
                arrival: "Home",
                arrivalTime: DateTime.UtcNow.AddHours(3),
                type: TransportType.Bike,
                distanceValue: 25.5m
            );

            journey.MarkAsGoalAchiever(); // Mark daily goal achieved
            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Assert - Journey marked with daily goal achievement
            var savedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            savedJourney!.Distance.Value.Should().BeGreaterThanOrEqualTo(20m);
            savedJourney.IsDailyGoalAchieved.Should().BeTrue();
        }

        [Fact]
        public async Task Journey_Share_With_User_Integration()
        {
            // Arrange - REQUIREMENT U-4: Sharing with specific users
            using var context = CreateDbContext();
            const string ownerId = "owner-oid";
            const string shareeId = "sharee-oid";

            var journey = Journey.Create(
                userId: ownerId,
                ownerId: ownerId,
                start: "Point A",
                startTime: DateTime.UtcNow,
                arrival: "Point B",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Bike,
                distanceValue: 15.5m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Act - Share with another user
            journey.ShareWithUser(shareeId);
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Journey is shared
            var updatedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            updatedJourney!.SharedWithUsers.Should()
       .ContainSingle(s => s.SharedWithUserId == shareeId);
            updatedJourney.SharedWithUsers.Should().HaveCount(1);
        }

        [Fact]
        public async Task Journey_Share_Is_Idempotent_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string ownerId = "owner-oid";
            const string shareeId = "sharee-oid";

            var journey = Journey.Create(
                userId: ownerId,
                ownerId: ownerId,
                start: "Point A",
                startTime: DateTime.UtcNow,
                arrival: "Point B",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Walk,
                distanceValue: 8.5m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Act - Share multiple times with same user
            journey.ShareWithUser(shareeId);
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            journey.ShareWithUser(shareeId); // Second share attempt
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Only one share recorded (idempotent)
            var updatedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

         
            updatedJourney!.SharedWithUsers.Count(s => s.SharedWithUserId == shareeId).Should().Be(1);

        }

        [Fact]
        public async Task Journey_Public_Link_Generation_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string ownerId = "owner-oid";

            var journey = Journey.Create(
                userId: ownerId,
                ownerId: ownerId,
                start: "Public Start",
                startTime: DateTime.UtcNow,
                arrival: "Public End",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Bike,
                distanceValue: 12.5m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Act - Generate public link
            journey.GeneratePublicLink();
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Public link created and accessible
            var updatedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            updatedJourney!.PublicSharingToken.Should().NotBeNullOrEmpty();
            updatedJourney.IsPublicLinkRevoked.Should().BeFalse();
            Guid.TryParse(updatedJourney.PublicSharingToken, out _).Should().BeTrue();
        }

        [Fact]
        public async Task Journey_Public_Link_Revocation_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string ownerId = "owner-oid";

            var journey = Journey.Create(
                userId: ownerId,
                ownerId: ownerId,
                start: "Start",
                startTime: DateTime.UtcNow,
                arrival: "End",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Bike,
                distanceValue: 10m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Act - Generate then revoke public link
            journey.GeneratePublicLink();
            var tokenBeforeRevoke = journey.PublicSharingToken;
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            journey.RevokePublicLink();
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Link is revoked but token preserved for audit
            var revokedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            revokedJourney!.IsPublicLinkRevoked.Should().BeTrue();
            revokedJourney.PublicSharingToken.Should().Be(tokenBeforeRevoke);
        }

        [Fact]
        public async Task Journey_Favorite_Removal_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string ownerId = "owner-oid";
            const string favoriterId = "favoriter-oid";

            var journey = Journey.Create(
                userId: ownerId,
                ownerId: ownerId,
                start: "Location",
                startTime: DateTime.UtcNow,
                arrival: "Destination",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Bike,
                distanceValue: 7.5m
            );

            journey.AddFavorite(favoriterId);
            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            // Act - Remove favorite
            journey.RemoveFavorite(favoriterId);
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Favorite removed
            var updatedJourney = await context.Journeys
                .Include(j => j.Favorites)
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            updatedJourney!.Favorites.Should().HaveCount(0);
        }

        [Fact]
        public async Task Journey_Update_Persists_Changes_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            var journey = Journey.Create(
                userId: testUserId,
                ownerId: ownerId,
                start: "Original Start",
                startTime: DateTime.UtcNow,
                arrival: "Original Arrival",
                arrivalTime: DateTime.UtcNow.AddHours(1),
                type: TransportType.Bike,
                distanceValue: 10m
            );

            context.Journeys.Add(journey);
            await context.SaveChangesAsync();

            var originalDistance = journey.Distance.Value;

            // Act - Update journey
            journey.Update(
                start: "Updated Start",
                startTime: DateTime.UtcNow,
                arrival: "Updated Arrival",
                arrivalTime: DateTime.UtcNow.AddHours(2),
                type: TransportType.Bike,
                distance: 20.5m
            );
            context.Journeys.Update(journey);
            await context.SaveChangesAsync();

            // Assert - Changes persisted
            var updatedJourney = await context.Journeys
                .FirstOrDefaultAsync(j => j.Id == journey.Id);

            updatedJourney!.StartLocation.Should().Be("Updated Start");
            updatedJourney.ArrivalLocation.Should().Be("Updated Arrival");
            updatedJourney.Distance.Value.Should().Be(20.5m);
            updatedJourney.Distance.Value.Should().NotBe(originalDistance);
        }

        [Fact]
        public async Task Multiple_Journeys_Per_User_Integration()
        {
            // Arrange - REQUIREMENT U-2: Multiple journeys per user
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            var journeys = new List<Journey>
            {
                Journey.Create(testUserId, ownerId, "Park", DateTime.UtcNow, "Gym", DateTime.UtcNow.AddHours(1), TransportType.Bike, 10.5m),
                Journey.Create(testUserId, ownerId, "Home", DateTime.UtcNow.AddHours(12), "Office", DateTime.UtcNow.AddHours(13), TransportType.Bike, 8.5m),
                Journey.Create(testUserId, ownerId, "Office", DateTime.UtcNow.AddHours(20), "Home", DateTime.UtcNow.AddHours(21), TransportType.Walk, 5.5m)
            };

            // Act
            context.Journeys.AddRange(journeys);
            await context.SaveChangesAsync();

            // Assert - All journeys saved and retrievable
            var userJourneys = await context.Journeys
                .Where(j => j.UserId == testUserId)
                .ToListAsync();

            userJourneys.Should().HaveCount(3);
            userJourneys.Sum(j => j.Distance.Value).Should().Be(24.5m);
        }

        [Fact]
        public async Task Journey_Deletion_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            var journey1 = Journey.Create(testUserId, ownerId, "Start1", DateTime.UtcNow, "End1", DateTime.UtcNow.AddHours(1), TransportType.Bike, 10m);
            var journey2 = Journey.Create(testUserId, ownerId, "Start2", DateTime.UtcNow.AddHours(2), "End2", DateTime.UtcNow.AddHours(3), TransportType.Bike, 15m);

            context.Journeys.AddRange(journey1, journey2);
            await context.SaveChangesAsync();

            var journeyToDeleteId = journey2.Id;

            // Act - Delete journey
            context.Journeys.Remove(journey2);
            await context.SaveChangesAsync();

            // Assert - Deleted journey not in list
            var userJourneys = await context.Journeys
                .Where(j => j.UserId == testUserId)
                .ToListAsync();

            userJourneys.Should().HaveCount(1);
            userJourneys[0].Id.Should().Be(journey1.Id);
            userJourneys.Any(j => j.Id == journeyToDeleteId).Should().BeFalse();
        }

        [Fact]
        public async Task Journey_Filtering_By_Date_Range_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            var now = DateTime.UtcNow;
            var journeys = new List<Journey>
            {
                Journey.Create(testUserId, ownerId, "Old", now.AddDays(-10), "Old End", now.AddDays(-10).AddHours(1), TransportType.Bike, 10m),
                Journey.Create(testUserId, ownerId, "Recent", now.AddDays(-1), "Recent End", now.AddDays(-1).AddHours(1), TransportType.Bike, 20m),
                Journey.Create(testUserId, ownerId, "Today", now, "Today End", now.AddHours(1), TransportType.Walk, 15m)
            };

            context.Journeys.AddRange(journeys);
            await context.SaveChangesAsync();

            // Act - Filter recent journeys (last 3 days)
            var startDate = now.AddDays(-3);
            var endDate = now;
            var recentJourneys = await context.Journeys
                .Where(j => j.UserId == testUserId && j.StartTime >= startDate && j.StartTime <= endDate)
                .OrderByDescending(j => j.StartTime)
                .ToListAsync();

            // Assert
            recentJourneys.Should().HaveCount(2);
            recentJourneys[0].StartLocation.Should().Be("Today");
            recentJourneys[1].StartLocation.Should().Be("Recent");
        }

        [Fact]
        public async Task Journey_Total_Distance_Aggregation_Integration()
        {
            // Arrange
            using var context = CreateDbContext();
            const string testUserId = "test-user-oid";
            const string ownerId = "owner-oid";

            var journeys = new List<Journey>
            {
                Journey.Create(testUserId, ownerId, "S1", DateTime.UtcNow, "E1", DateTime.UtcNow.AddHours(1), TransportType.Bike, 5.5m),
                Journey.Create(testUserId, ownerId, "S2", DateTime.UtcNow.AddHours(2), "E2", DateTime.UtcNow.AddHours(3), TransportType.Bike, 12.3m),
                Journey.Create(testUserId, ownerId, "S3", DateTime.UtcNow.AddHours(4), "E3", DateTime.UtcNow.AddHours(5), TransportType.Walk, 7.2m)
            };

            context.Journeys.AddRange(journeys);
            await context.SaveChangesAsync();

            // Act - Calculate total distance for user
            var totalDistance = await context.Journeys
                .Where(j => j.UserId == testUserId)
                .SumAsync(j => j.Distance.Value);

            // Assert
            totalDistance.Should().Be(25m);
        }
    }
}
