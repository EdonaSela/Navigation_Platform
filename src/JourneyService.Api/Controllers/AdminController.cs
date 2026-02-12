using Azure;
using JourneyService.Application.Commands.User;
using JourneyService.Application.Journeys.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace JourneyService.Api.Controllers
{
    [Authorize(Policy = "RequireAdminScope")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IMediator mediator, ILogger<AdminController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("journeys")]
        public async Task<IActionResult> GetAdminJourneys([FromQuery] GetAdminJourneysQuery query)
        {
            _logger.LogInformation("Admin journeys requested. Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);
            var result = await _mediator.Send(query);

          
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Ok(result.Items);
        }

        [HttpGet("statistics/monthly-distance")]
        public async Task<ActionResult<IEnumerable<MonthlyDistanceDto>>> GetAdminStats(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? orderBy = null)
        {
            _logger.LogInformation("Admin monthly stats requested. Page: {Page}, PageSize: {PageSize}, OrderBy: {OrderBy}", page, pageSize, orderBy);
            var query = new GetMonthlyStatsQuery
            {
                Page = page,
                PageSize = pageSize,
                OrderBy = orderBy
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("users")] 
        public async Task<ActionResult<List<UserListItemDto>>> GetUsers()
        {
            _logger.LogInformation("Admin users list requested.");
            var result = await _mediator.Send(new GetUsersQuery());
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] StatusUpdateRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("oid")
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(adminId))
            {
                _logger.LogWarning("User status update forbidden. USer: {UserId}", id);
                return Forbid();
            }

            _logger.LogInformation("Admin {AdminId} updating user status. USer: {UserId}, Status: {Status}", adminId, id, request.Status);
            
            await _mediator.Send(new UpdateUserStatusCommand(id, request.Status, adminId));

            return NoContent();
        }
    }
}
