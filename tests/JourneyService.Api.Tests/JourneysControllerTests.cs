using FluentAssertions;
using JourneyService.Api.Controllers;
using JourneyService.Application.Commands.Notification;
using JourneyService.Application.Commands.Sharing;
using JourneyService.Application.Journeys.Commands;
using JourneyService.Application.Journeys.Queries;
using JourneyService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class JourneysControllerTests
{
    [Fact]
    public async Task Create_Returns_CreatedAtAction_With_Id()
    {
        var mediator = new Mock<IMediator>();

        var logger = new Mock<ILogger<JourneysController>>();
        var newId = Guid.NewGuid();
        mediator.Setup(m => m.Send(It.IsAny<CreateJourneyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        var controller = new JourneysController(mediator.Object,logger.Object);

        var cmd = new CreateJourneyCommand("A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), TransportType.Car, 12m);
        var result = await controller.Create(cmd);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.RouteValues.Should().ContainKey("id");
    }

    [Fact]
    public async Task GetById_Returns_NotFound_When_Null()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<GetJourneyByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JourneyDto?)null);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Returns_Ok_When_Found()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        var dto = new JourneyDto(Guid.NewGuid(), "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), "Car", 5m, false, false, null, new List<JourneyFavoriteDto>());
        mediator.Setup(m => m.Send(It.IsAny<GetJourneyByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.GetById(dto.Id);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetPaged_Returns_Ok_With_Result()
    {
        var mediator = new Mock<IMediator>();
        var response = new List<JourneyDto>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<GetPagedJourneysQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.GetPaged(1, 20);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task Update_Returns_BadRequest_When_Id_Does_Not_Match()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        var controller = new JourneysController(mediator.Object, logger.Object);
        var routeId = Guid.NewGuid();
        var payloadId = Guid.NewGuid();
        var command = new UpdateJourneyCommand(payloadId, "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), TransportType.Car, 10m);

        var result = await controller.Update(routeId, command);

        result.Should().BeOfType<BadRequestObjectResult>();
        mediator.Verify(m => m.Send(It.IsAny<UpdateJourneyCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_Returns_NoContent_When_Id_Matches()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateJourneyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);
        var id = Guid.NewGuid();
        var command = new UpdateJourneyCommand(id, "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), TransportType.Car, 10m);

        var result = await controller.Update(id, command);

        result.Should().BeOfType<NoContentResult>();
        mediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Returns_NoContent()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<DeleteJourneyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Share_Returns_NoContent()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<ShareJourneyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);
        

        var request = new ShareJourneyRequest
        {
            Emails = new List<string> { "u1", "u2" }
        };

        var result = await controller.Share(Guid.NewGuid(), request);
        result.Should().BeOfType<NoContentResult>();


        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetByPublicToken_Returns_Ok()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        var dto = new JourneyDto(Guid.NewGuid(), "user1", "A", DateTime.UtcNow, "B", DateTime.UtcNow.AddHours(1), "Car", 5m, false, false, null, new List<JourneyFavoriteDto>());
        mediator.Setup(m => m.Send(It.IsAny<GetJourneyByPublicTokenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.GetByPublicToken("token-1");

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GeneratePublicLink_Returns_Ok_With_Url()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<GeneratePublicLinkCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("public-token");

        var controller = new JourneysController(mediator.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GeneratePublicLink(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().NotBeNull();
        ok.Value!.GetType().GetProperty("url")!.GetValue(ok.Value)!.ToString()
            .Should().Be("http://localhost:4200/shared/public-token");
    }

    [Fact]
    public async Task RevokePublicLink_Returns_NoContent()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<RevokePublicLinkCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.RevokePublicLink(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task FavoriteJourney_Returns_Ok()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<FavoriteJourneyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.FavoriteJourney(Guid.NewGuid());

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task UnFavoriteJourney_Returns_Ok()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JourneysController>>();
        mediator.Setup(m => m.Send(It.IsAny<UnfavoriteJourneyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new JourneysController(mediator.Object, logger.Object);

        var result = await controller.UnFavoriteJourney(Guid.NewGuid());

        result.Should().BeOfType<OkResult>();
    }
}
