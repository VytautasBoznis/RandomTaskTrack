using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class CreatePlantScheduleRequestValidator : AbstractValidator<CreatePlantScheduleRequest>
{
    public CreatePlantScheduleRequestValidator()
    {
        RuleFor(x => x.PlantId).NotEmpty();
        RuleFor(x => x.Tasks).NotEmpty();

        RuleForEach(x => x.Tasks).ChildRules(task =>
        {
            task.RuleFor(t => t.Title).NotEmpty().MaximumLength(200);

            // A year is the far end of plant care (repotting). Anything beyond
            // it is a typo, and the recurrence would sit there doing nothing.
            task.RuleFor(t => t.IntervalDays).InclusiveBetween(1, 365);
        });
    }
}
