using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Jorneys.Exceptions;
using JourneyService.Application.Journeys.Exceptions;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace JourneyService.Application.Commands.Sharing
{
    public record RevokePublicLinkCommand(Guid Id) : IRequest;

    public class RevokePublicLinkCommandHandler : IRequestHandler<RevokePublicLinkCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RevokePublicLinkCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(RevokePublicLinkCommand request, CancellationToken cancellationToken)
        {
            var journey = await _context.Journeys
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

         
            if (journey == null)
                throw new NotFoundException("Journey", request.Id);

       
            if (journey.UserId != _currentUserService.UserId)
                throw new ForbiddenAccessException();

         
            journey.RevokePublicLink();

          

            await _context.SaveChangesAsync(cancellationToken);


            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUserService.UserId,
                Action = "Revoke Public Token",
                JourneyId = journey.Id
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
