using MediatR;

namespace JourneyService.Application.Journeys.Commands;

public record DeleteJourneyCommand(Guid Id) : IRequest;