using MediatR;
using JourneyService.Domain.Enums;

namespace JourneyService.Application.Journeys.Commands;

public record UpdateJourneyCommand(
    Guid Id,
    string StartLocation,
    DateTime StartTime,
    string ArrivalLocation,
    DateTime ArrivalTime,
    TransportType TransportType,
    decimal DistanceKm) : IRequest;