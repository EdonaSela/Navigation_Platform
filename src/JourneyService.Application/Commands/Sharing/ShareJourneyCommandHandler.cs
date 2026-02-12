using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Jorneys.Exceptions;
using JourneyService.Application.Journeys.Exceptions;
using JourneyService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;

public record ShareJourneyCommand(Guid JourneyId, List<string> Emails) : IRequest;

public class ShareJourneyCommandHandler : IRequestHandler<ShareJourneyCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser; 

    public ShareJourneyCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ShareJourneyCommand request, CancellationToken ct)
    {
        var journey = await _context.Journeys
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, ct);

        if (journey == null) throw new NotFoundException();
        if (journey.UserId != _currentUser.UserId) throw new ForbiddenAccessException();

        var normalizedEmails = request.Emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedEmails.Count == 0)
        {
            return;
        }

        var usersToShareWith = await _context.Users
            .Where(u => normalizedEmails.Contains(u.Email))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        var foundEmails = usersToShareWith
            .Select(u => u.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var firstMissingEmail = normalizedEmails.FirstOrDefault(e => !foundEmails.Contains(e));
        if (!string.IsNullOrWhiteSpace(firstMissingEmail))
        {
            throw new NotFoundException("User", firstMissingEmail);
        }

        foreach (var user in usersToShareWith)
        {
            journey.ShareWithUser(user.Id);
        }

        await _context.SaveChangesAsync(ct);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            Action = "Share",
            JourneyId = journey.Id
        });
        await _context.SaveChangesAsync(ct);
    }
}
