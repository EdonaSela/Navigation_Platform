using JourneyService.Domain.Enums;
using MediatR;
//using JourneyService.Domain.Entities; // You will create the Journey entity here
// using JourneyService.Application.Common.Interfaces; // For your DB Context

namespace JourneyService.Application.Journeys.Commands;


public record CreateJourneyCommand(
    string StartLocation,
    DateTime StartTime,
    string ArrivalLocation,
    DateTime ArrivalTime,
    TransportType TransportType,
    decimal DistanceKm) : IRequest<Guid>;

