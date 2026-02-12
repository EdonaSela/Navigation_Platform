
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Jorneys.Exceptions;
using JourneyService.Application.Journeys.Exceptions;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Application.Journeys.Commands;

//  tells MediatR to expect a string return value
public record GeneratePublicLinkCommand(Guid Id) : IRequest<string>;

public class GeneratePublicLinkCommandHandler : IRequestHandler<GeneratePublicLinkCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GeneratePublicLinkCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(GeneratePublicLinkCommand request, CancellationToken cancellationToken)
    {
    
        var journey = await _context.Journeys
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

    
        if (journey == null)
            throw new NotFoundException("Journey", request.Id);

     
        if (journey.UserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        journey.GeneratePublicLink();


        await _context.SaveChangesAsync(cancellationToken);

    

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "GeneratePublicoken",
            JourneyId = journey.Id
        });
        await _context.SaveChangesAsync(cancellationToken);

        return journey.PublicSharingToken!;
    }
}