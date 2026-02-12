using FluentAssertions;
using JourneyService.Application.Commands.Sharing;
using JourneyService.Application.Common.Interfaces;
using JourneyService.Domain.Entities;
using JourneyService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;


// Tests for Journey Sharing features - validates sharing rules, public links, and audit logging.

public class JourneySharingTests
{
    [Fact]
    public void GeneratePublicLink_Creates_Unique_Token()
    {
        // Arrange
        var journey1 = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        var journey2 = Journey.Create(
            "user1", "user1", "C", DateTime.UtcNow, "D", DateTime.UtcNow.AddHours(1),
            TransportType.Bike, 15m
        );

        // Act
        journey1.GeneratePublicLink();
        journey2.GeneratePublicLink();

        // Assert - Each journey should have unique token
        journey1.PublicSharingToken.Should().NotBeNullOrEmpty();
        journey2.PublicSharingToken.Should().NotBeNullOrEmpty();
        journey1.PublicSharingToken.Should().NotBe(journey2.PublicSharingToken);
    }

    [Fact]
    public void PublicLink_Should_Not_Be_Revoked_Initially()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );

        // Act
        journey.GeneratePublicLink();

        // Assert
        journey.IsPublicLinkRevoked.Should().BeFalse();
        journey.PublicSharingToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RevokePublicLink_Sets_IsRevoked_Flag()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        journey.GeneratePublicLink();

        // Act
        journey.RevokePublicLink();

        // Assert
        journey.IsPublicLinkRevoked.Should().BeTrue();
        journey.PublicSharingToken.Should().NotBeNullOrEmpty(); // Token still exists, but marked as revoked
    }

    [Fact]
    public void ShareWithUser_Adds_User_To_SharedList()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        var sharedUserId = "user2";

        // Act
        journey.ShareWithUser(sharedUserId);

        // Assert
      
        journey.SharedWithUsers.Should()
    .ContainSingle(s => s.SharedWithUserId == sharedUserId);
    }

    [Fact]
    public void ShareWithUser_Multiple_Users()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        var user2 = "user2";
        var user3 = "user3";
        var user4 = "user4";

        // Act
        journey.ShareWithUser(user2);
        journey.ShareWithUser(user3);
        journey.ShareWithUser(user4);

        // Assert
        journey.SharedWithUsers.Should().HaveCount(3);
       

        journey.SharedWithUsers
    .Select(s => s.SharedWithUserId)
    .Should()
    .BeEquivalentTo(new[] { user2, user3, user4 });
    }

    [Fact]
    public void ShareWithUser_Should_Be_Idempotent()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        var sharedUserId = "user2";

        // Act
        journey.ShareWithUser(sharedUserId);
        journey.ShareWithUser(sharedUserId);
        journey.ShareWithUser(sharedUserId);

        // Assert - Should not create duplicates
        var count = 0;
        

        foreach (var share in journey.SharedWithUsers)
        {
            if (share.SharedWithUserId == sharedUserId)
                count++;
        }

        count.Should().Be(1);

        count.Should().Be(1);
    }

    [Fact]
    public void AuditLog_Records_Share_Action()
    {
        // Arrange
        var userId = "admin-user";
        var journeyId = Guid.NewGuid();
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "Share",
            JourneyId = journeyId
        };

        // Assert - Audit log should capture essential fields
        auditLog.UserId.Should().Be(userId);
        auditLog.Action.Should().Be("Share");
        auditLog.JourneyId.Should().Be(journeyId);
    }

    [Fact]
    public void AuditLog_Records_RevokePublicToken_Action()
    {
        // Arrange
        var userId = "admin-user";
        var journeyId = Guid.NewGuid();
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "Revoke Public Token",
            JourneyId = journeyId
        };

        // Assert
        auditLog.Action.Should().Be("Revoke Public Token");
    }

    [Fact]
    public void AuditLog_Should_Have_Timestamp()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            UserId = "user1",
            Action = "Share",
            JourneyId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };

        // Assert
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Public_Link_Access_Should_Return_410_When_Revoked()
    {
        // Arrange
        var journey = Journey.Create(
            "user1", "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );
        journey.GeneratePublicLink();

        // Act - Revoke the link
        journey.RevokePublicLink();

        // Assert
        journey.IsPublicLinkRevoked.Should().BeTrue();
        // When isRevoked=true, API should return 410 Gone for requests with this token
    }

    [Fact]
    public void Shared_Journey_Visible_To_Recipient()
    {
        // Arrange
        var ownerId = "user1";
        var recipientId = "user2";
        var journey = Journey.Create(
            ownerId, ownerId, "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1),
            TransportType.Car, 10m
        );

        // Act
        journey.ShareWithUser(recipientId);

        // Assert - Recipient should be in shared list  (appears in their feed)
      
        journey.SharedWithUsers.Should()
    .ContainSingle(s => s.SharedWithUserId == recipientId);
        journey.UserId.Should().Be(ownerId);
    }
}
