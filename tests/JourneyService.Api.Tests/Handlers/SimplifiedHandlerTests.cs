using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Commands;
using JourneyService.Domain.Entities;
using JourneyService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;


// Tests for CreateJourneyCommandHandler - validates journey creation, user authorization, and domain event publishing.

public class CreateJourneyCommandHandlerSimplifiedTests
{
    [Fact]
    public async Task Handle_Creates_Journey_With_Valid_UserId()
    {
        // Arrange
        var userId = "user-123";
        var command = new CreateJourneyCommand(
            "Tirana, AL", DateTime.UtcNow.AddDays(-1), "Durrës, AL", 
            DateTime.UtcNow.AddDays(-1).AddHours(1), TransportType.Car, 38.40m
        );

        var mockContext = new Mock<IApplicationDbContext>();
        var mockHttpContext = new Mock<IHttpContextAccessor>();
        var mockHubNotifier = new Mock<IHubNotifier>();
        var mockPublisher = new Mock<IPublisher>();

        var journeys = new List<Journey>();
        mockContext.Setup(x => x.Journeys.Add(It.IsAny<Journey>()))
            .Callback<Journey>(j => journeys.Add(j));
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("oid", userId) }))
        };
        mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);

        var handler = new CreateJourneyCommandHandler(
            mockContext.Object, mockHttpContext.Object, mockHubNotifier.Object, mockPublisher.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        journeys.Should().HaveCount(1);
        journeys[0].UserId.Should().Be(userId);
        journeys[0].StartLocation.Should().Be("Tirana, AL");
    }

    [Fact]
    public async Task Handle_Throws_UnauthorizedAccessException_When_No_UserId()
    {
        // Arrange
        var command = new CreateJourneyCommand(
            "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), TransportType.Car, 10m
        );

        var mockContext = new Mock<IApplicationDbContext>();
        var mockHttpContext = new Mock<IHttpContextAccessor>();
        var mockHubNotifier = new Mock<IHubNotifier>();
        var mockPublisher = new Mock<IPublisher>();

        var emptyHttpContext = new DefaultHttpContext();
        mockHttpContext.Setup(x => x.HttpContext).Returns(emptyHttpContext);

        var handler = new CreateJourneyCommandHandler(
            mockContext.Object, mockHttpContext.Object, mockHubNotifier.Object, mockPublisher.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Publishes_JourneyCreatedEvent()
    {
        // Arrange
        var userId = "user-456";
        var command = new CreateJourneyCommand(
            "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), TransportType.Bike, 15m
        );

        var mockContext = new Mock<IApplicationDbContext>();
        var mockHttpContext = new Mock<IHttpContextAccessor>();
        var mockHubNotifier = new Mock<IHubNotifier>();
        var mockPublisher = new Mock<IPublisher>();

        mockContext.Setup(x => x.Journeys.Add(It.IsAny<Journey>()));
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }))
        };
        mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);

        var handler = new CreateJourneyCommandHandler(
            mockContext.Object, mockHttpContext.Object, mockHubNotifier.Object, mockPublisher.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - verify that Publish was invoked once with any INotification
        mockPublisher.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        mockHubNotifier.Verify(x => x.SendJourneyCreated(It.IsAny<Journey>()), Times.Once);
    }
}
