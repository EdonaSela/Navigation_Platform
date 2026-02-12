using JourneyService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JourneyService.Application.Journeys.Queries
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserListItemDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetUsersHandler> _logger;

        public GetUsersHandler(IApplicationDbContext context, ILogger<GetUsersHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Handling users list query.");
            var users = await _context.Users
                .Select(u => new UserListItemDto(u.Id, u.Email, u.Status.ToString(), u.CreatedAt))
                .ToListAsync(ct);

            _logger.LogInformation("Users list query completed. ReturnedUsers: {ReturnedUsers}", users.Count);
            return users;
        }
    }
}
