
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Journey.Exceptions;
using JourneyService.Application.Journeys.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
namespace JourneyService.Application.Journeys.Queries;

public record GetJourneyByPublicTokenQuery(string Token) : IRequest<JourneyDto>;
public class GetJourneyByPublicTokenQueryHandler : IRequestHandler<GetJourneyByPublicTokenQuery, JourneyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper; 

    public GetJourneyByPublicTokenQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<JourneyDto> Handle(GetJourneyByPublicTokenQuery request, CancellationToken cancellationToken)
    {
        
        var journey = await _context.Journeys
            .FirstOrDefaultAsync(x => x.PublicSharingToken == request.Token, cancellationToken);

      
        if (journey == null)
            throw new NotFoundException("Journey", request.Token);

       
        if (journey.IsPublicLinkRevoked)
        {
            throw new GoneException("This sharing link has been revoked by the owner.");
        }



        
        return _mapper.Map<JourneyDto>(journey);
    }
}