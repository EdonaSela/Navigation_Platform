using FluentAssertions;
using JourneyService.Api.Controllers;
using JourneyService.Application.Commands.User;
using JourneyService.Application.Common.Models;
using JourneyService.Application.Journeys.Queries;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class AdminControllerTests
{
    [Fact]
    public async Task GetAdminJourneys_Returns_Ok_With_Items()
    {
        var mediator = new Mock<IMediator>();

        var logger = new Mock<ILogger<AdminController>>();
        var dto = new JourneyDto(Guid.NewGuid(), "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), "Car", 5m, false, false, null, new List<JourneyFavoriteDto>());
        var response = new AdminJourneysResponse { Items = new List<JourneyDto> { dto }, TotalCount = 1 };
        
        // Setup mock to handle GetAdminJourneysQuery
        mediator.Setup(m => m.Send(It.IsAny<GetAdminJourneysQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AdminController(mediator.Object, logger.Object);
        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var result = await controller.GetAdminJourneys(new GetAdminJourneysQuery());

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeEquivalentTo(new List<JourneyDto> { dto });
    }

    [Fact]
    public async Task UpdateUserStatus_Returns_NoContent()
    {
        var mediator = new Mock<IMediator>();

        var logger = new Mock<ILogger<AdminController>>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateUserStatusCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new AdminController(mediator.Object, logger.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1") }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var request = new StatusUpdateRequest { Status = UserStatus.Suspended };
        var result = await controller.UpdateUserStatus("u1", request);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetAdminStats_Returns_Ok_With_Result()
    {
        var mediator = new Mock<IMediator>();

        var logger = new Mock<ILogger<AdminController>>();
        var stats = new List<MonthlyDistanceDto>
        {
            new() { Year = 2026, Month = 1, TotalDistanceKm = 100 },
            new() { Year = 2026, Month = 2, TotalDistanceKm = 150 }
        };
        var paged = new PagedList<MonthlyDistanceDto>(stats, stats.Count, 1, 20);

        mediator.Setup(m => m.Send(It.IsAny<GetMonthlyStatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var controller = new AdminController(mediator.Object,logger.Object);

        var result = await controller.GetAdminStats(1, 20, "month");

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().BeEquivalentTo(paged);
    }

    [Fact]
    public async Task GetUsers_Returns_Ok_With_Result()
    {
        var mediator = new Mock<IMediator>();

        var logger = new Mock<ILogger<AdminController>>();
        var users = new List<UserListItemDto>
        {
            new("u1", "u1@example.com", "Active", DateTime.UtcNow)
        };

        mediator.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var controller = new AdminController(mediator.Object, logger.Object);

        var result = await controller.GetUsers();

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().BeEquivalentTo(users);
    }
}
