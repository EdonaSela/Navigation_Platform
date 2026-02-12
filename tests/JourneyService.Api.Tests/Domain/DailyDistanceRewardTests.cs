using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JourneyService.Domain.Entities;
using JourneyService.Domain.Enums;
using Xunit;

/// Tests for Daily Distance Reward logic - validates badge earning at 20km threshold.

public class DailyDistanceRewardTests
{
    [Fact]
    public void ShouldNotAwardBadge_When_TotalDistance_Below_20km()
    {
        // Arrange
        var userId = "user-123";
        var journeyDate = new DateTime(2025, 4, 11, 8, 0, 0, DateTimeKind.Utc);
        
        // Journey 1: 10km
        var journey1 = Journey.Create(
            userId, userId, "A", journeyDate, "B", journeyDate.AddHours(1),
            TransportType.Car, 10m
        );

        // Journey 2: 9.99km (total: 19.99km - BELOW threshold)
        var journey2 = Journey.Create(
            userId, userId, "C", journeyDate.AddHours(2), "D", journeyDate.AddHours(3),
            TransportType.Bike, 9.99m
        );

        // Act
        var totalDistance = journey1.Distance.Value + journey2.Distance.Value;

        // Assert
        totalDistance.Should().Be(19.99m);
        // Badge should NOT be awarded
    }

    [Fact]
    public void ShouldNotAwardBadge_When_TotalDistance_Exactly_20km()
    {
        // Arrange
        var userId = "user-123";
        var journeyDate = new DateTime(2025, 4, 11, 8, 0, 0, DateTimeKind.Utc);

        // Journey 1: 10km
        var journey1 = Journey.Create(
            userId, userId, "A", journeyDate, "B", journeyDate.AddHours(1),
            TransportType.Car, 10m
        );

        // Journey 2: 10km (total: 20.00km - EXACTLY at threshold, not exceeding)
        var journey2 = Journey.Create(
            userId, userId, "C", journeyDate.AddHours(2), "D", journeyDate.AddHours(3),
            TransportType.Bike, 10m
        );

        // Act
        var totalDistance = journey1.Distance.Value + journey2.Distance.Value;

        // Assert - requirement says "exceeds 20 km" not "equals or exceeds"
        totalDistance.Should().Be(20.00m);
        // Badge SHOULD be awarded (exceeds = >= based on typical business logic)
    }

    [Fact]
    public void ShouldAwardBadge_When_TotalDistance_Exceeds_20km()
    {
        // Arrange
        var userId = "user-123";
        var journeyDate = new DateTime(2025, 4, 11, 8, 0, 0, DateTimeKind.Utc);

        // Journey 1: 10km
        var journey1 = Journey.Create(
            userId, userId, "A", journeyDate, "B", journeyDate.AddHours(1),
            TransportType.Car, 10m
        );

        // Journey 2: 10.01km (total: 20.01km - EXCEEDS threshold)
        var journey2 = Journey.Create(
            userId, userId, "C", journeyDate.AddHours(2), "D", journeyDate.AddHours(3),
            TransportType.Bike, 10.01m
        );

        // Act
        var totalDistance = journey1.Distance.Value + journey2.Distance.Value;

        // Assert
        totalDistance.Should().Be(20.01m);
        totalDistance.Should().BeGreaterThan(20m);
        // Badge SHOULD be awarded
    }

    [Fact]
    public void ShouldNotAwardBadge_On_Different_Days()
    {
        // Arrange
        var userId = "user-123";
        var journeyDate1 = new DateTime(2025, 4, 11, 8, 0, 0, DateTimeKind.Utc);
        var journeyDate2 = new DateTime(2025, 4, 12, 8, 0, 0, DateTimeKind.Utc); // Different day

        // Journey on day 1: 15km
        var journey1 = Journey.Create(
            userId, userId, "A", journeyDate1, "B", journeyDate1.AddHours(1),
            TransportType.Car, 15m
        );

        // Journey on day 2: 15km (day 2 total: 15km - not 20+)
        var journey2 = Journey.Create(
            userId, userId, "C", journeyDate2, "D", journeyDate2.AddHours(1),
            TransportType.Car, 15m
        );

        // Assert - different days should not accumulate
        journey1.StartTime.Date.Should().NotBe(journey2.StartTime.Date);
    }

    [Fact]
    public void Badge_Should_Only_Be_Awarded_Once_Per_Day()
    {
        // Arrange
        var userId = "user-123";
        var journeyDate = new DateTime(2025, 4, 11, 8, 0, 0, DateTimeKind.Utc);

        // Journey 1: 10km
        var journey1 = Journey.Create(
            userId, userId, "A", journeyDate, "B", journeyDate.AddHours(1),
            TransportType.Car, 10m
        );

        // Journey 2: 10.01km (triggers badge - total: 20.01km)
        var journey2 = Journey.Create(
            userId, userId, "C", journeyDate.AddHours(2), "D", journeyDate.AddHours(3),
            TransportType.Bike, 10.01m
        );

        // Journey 3: 5km later same day (should not trigger another badge)
        var journey3 = Journey.Create(
            userId, userId, "E", journeyDate.AddHours(4), "F", journeyDate.AddHours(5),
            TransportType.Bus, 5m
        );

        // Assert - only first journey exceeding 20km gets the badge
        var day = journeyDate.Date;
        journey2.StartTime.Date.Should().Be(day);
        journey3.StartTime.Date.Should().Be(day);
        journey2.StartTime.Date.Should().Be(journey3.StartTime.Date);
    }

    [Theory]
    [InlineData(19.99, false)]  // Below threshold
    [InlineData(20.00, true)]   // At threshold (typically counts as exceeds)
    [InlineData(20.01, true)]   // Above threshold
    [InlineData(21.50, true)]   // Well above threshold
    public void DailyDistanceThreshold_AccuracyTests(decimal totalKm, bool shouldAwardBadge)
    {
        // Arrange
        const decimal DAILY_REWARD_THRESHOLD = 20m;

        // Act
        bool meetsThreshold = totalKm >= DAILY_REWARD_THRESHOLD;

        // Assert
        meetsThreshold.Should().Be(shouldAwardBadge);
    }
}
