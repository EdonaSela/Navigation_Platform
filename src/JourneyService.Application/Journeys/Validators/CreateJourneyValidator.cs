using FluentValidation;
using JourneyService.Application.Journeys.Commands;

namespace JourneyService.Application.Journeys.Validators;

public class CreateJourneyValidator : AbstractValidator<CreateJourneyCommand>
{
    public CreateJourneyValidator()
    {
        RuleFor(x => x.StartLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ArrivalLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DistanceKm).GreaterThan(0).ScalePrecision(2, 5); 
        RuleFor(x => x.ArrivalTime).GreaterThan(x => x.StartTime)
            .WithMessage("Arrival must be after Start time.");
    }
}