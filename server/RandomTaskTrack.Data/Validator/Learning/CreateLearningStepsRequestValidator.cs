using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class CreateLearningStepsRequestValidator : AbstractValidator<CreateLearningStepsRequest>
{
    /// <summary>
    /// A drafted plan is a page or two; adding all of it at once is a real
    /// gesture. Beyond this it is not a path any more, and the AI clamps its own
    /// output well below it.
    /// </summary>
    private const int MaxSteps = 50;

    public CreateLearningStepsRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Steps).NotEmpty().Must(steps => steps.Count <= MaxSteps)
            .WithMessage($"No more than {MaxSteps} steps at a time.");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Title).NotEmpty().MaximumLength(300);
            step.RuleFor(s => s.Kind).IsInEnum();
            step.RuleFor(s => s.Notes).MaximumLength(4000);
            step.RuleFor(s => s.Provider).MaximumLength(200);
            step.RuleFor(s => s.Url).MaximumLength(1000);
            step.RuleFor(s => s.Cost).MaximumLength(100);

            // A thousand hours is half a working year on one step. Past that it
            // is a goal, not a step.
            step.RuleFor(s => s.Hours).InclusiveBetween(1, 1000).When(s => s.Hours.HasValue);
        });
    }
}
