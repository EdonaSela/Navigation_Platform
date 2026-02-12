using Microsoft.AspNetCore.Mvc;
using MediatR;
using JourneyService.Application.Auth.Commands;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        _logger.LogInformation("Register endpoint called for email: {Email}", command.Email);
        var result = await _mediator.Send(command);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for email: {Email}", command.Email);
        }

        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }

    //[HttpPost("login")]
    //public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    //{
    //    var success = await _mediator.Send(command);
    //    return success ? Ok(new { Message = "Logged in!" }) : Unauthorized();
    //}
}
