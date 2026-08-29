using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class CreateSowingPlanRequestValidator : AbstractValidator<CreateSowingPlanRequest>
{
    public CreateSowingPlanRequestValidator()
    {
        RuleFor(x => x.PlantId).NotEmpty();
        RuleFor(x => x.SowOn).NotEmpty();
        RuleFor(x => x.Steps).NotEmpty();

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Title).NotEmpty().MaximumLength(200);

            // Sowing day is 0. Three years out is past anything a packet plans
            // for, and a negative offset would date a task before the sowing.
            step.RuleFor(s => s.DayOffset).InclusiveBetween(0, 1095);
        });
    }
}
