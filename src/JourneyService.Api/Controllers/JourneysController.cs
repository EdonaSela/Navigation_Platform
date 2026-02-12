using JourneyService.Application.Commands.Notification;
using JourneyService.Application.Commands.Sharing;
using JourneyService.Application.Journeys.Commands;
using JourneyService.Application.Journeys.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace JourneyService.Api.Controllers;

[ApiController]
[Route("api/journeys")]
[Authorize] 
public class JourneysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JourneysController> _logger;

    public JourneysController(IMediator mediator, ILogger<JourneysController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJourneyCommand command)
    {
        _logger.LogInformation("Creating journey");
        var id = await _mediator.Send(command);
        
      
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Fetching journey by id: {JourneyId}", id);
        var query = new GetJourneyByIdQuery(id);
        var journey = await _mediator.Send(query);
        
        return journey != null ? Ok(journey) : NotFound();
    }

   
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Fetching paged journeys. Page: {Page}, PageSize: {PageSize}", page, pageSize);
        var query = new GetPagedJourneysQuery(page, pageSize);
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }

   
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJourneyCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        _logger.LogInformation("Updating journey: {JourneyId}", id);
        await _mediator.Send(command);
        
        
        return NoContent();
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Deleting journey: {JourneyId}", id);
        await _mediator.Send(new DeleteJourneyCommand(id));

        return NoContent();
    }



    [HttpPost("{id}/share")]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareJourneyRequest request)
    {
        _logger.LogInformation("Sharing journey: {JourneyId} with {EmailCount} recipients", id, request.Emails?.Count ?? 0);
        await _mediator.Send(new ShareJourneyCommand(id, request.Emails ?? new List<string>()));
        return NoContent();
    }

    [AllowAnonymous] 
    [HttpGet("shared/{token}")]
    public async Task<IActionResult> GetByPublicToken(string token)
    {
        _logger.LogInformation("Fetching shared journey by token.");
        var journey = await _mediator.Send(new GetJourneyByPublicTokenQuery(token));
        return Ok(journey);
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicJourneys([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Fetching public journeys. Page: {Page}, PageSize: {PageSize}", page, pageSize);
        var result = await _mediator.Send(new GetPublicJourneysQuery(page, pageSize));
        return Ok(result);
    }

    [HttpPost("{id}/public-link")]
    public async Task<IActionResult> GeneratePublicLink(Guid id)
    {
        _logger.LogInformation("Generating public link for journey: {JourneyId}", id);
        
        var token = await _mediator.Send(new GeneratePublicLinkCommand(id));

    
        var scheme = Request.Scheme;
        var host = Request.Host;

        var publicUrl = $"http://localhost:4200/shared/{token}";


        return Ok(new { url = publicUrl });
    }
    [HttpDelete("{id}/public-link")]
    public async Task<IActionResult> RevokePublicLink(Guid id)
    {
        _logger.LogInformation("Revoking public link for journey: {JourneyId}", id);
        await _mediator.Send(new RevokePublicLinkCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/favorite")]
    public async Task<IActionResult> FavoriteJourney(Guid id)
    {
        _logger.LogInformation("Marking journey as favorite: {JourneyId}", id);


        await _mediator.Send(new FavoriteJourneyCommand(id));

        return Ok();
    }

    [HttpDelete("{id}/favorite")]
    public async Task<IActionResult> UnFavoriteJourney(Guid id)
    {
        _logger.LogInformation("Removing favorite mark from journey: {JourneyId}", id);


        await _mediator.Send(new UnfavoriteJourneyCommand(id));

        return Ok();
    }
}

public sealed class ShareJourneyRequest
{
    public List<string> Emails { get; set; } = new();
}
