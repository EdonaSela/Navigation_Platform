using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journeys.Exceptions;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.User
{
    public class UpdateUserStatusHandler : IRequestHandler<UpdateUserStatusCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateUserStatusHandler(IApplicationDbContext context, IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) throw new NotFoundException(nameof(UserProfile), request.UserId);
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? throw new UnauthorizedAccessException();
            var oldStatus = user.Status;

            user.Status = request.NewStatus;

            _context.UserStatusAudits.Add(new UserStatusAudit
            {
                UserId = request.UserId,
                OldStatus = oldStatus,
                NewStatus = request.NewStatus,
                ChangedByAdminId = userId,
                ChangedAt = DateTime.UtcNow
            });

            
            await _context.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new UserStatusChangedEvent(request.UserId, request.NewStatus), cancellationToken);
        }
    }
}
