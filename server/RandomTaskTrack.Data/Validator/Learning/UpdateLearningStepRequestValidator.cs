using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class UpdateLearningStepRequestValidator : AbstractValidator<UpdateLearningStepRequest>
{
    public UpdateLearningStepRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(4000);

        // Room for a mark breakdown and what went wrong, not an essay.
        RuleFor(x => x.Outcome).MaximumLength(4000);

        RuleFor(x => x.Provider).MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(1000);
        RuleFor(x => x.Cost).MaximumLength(100);
        RuleFor(x => x.Hours).InclusiveBetween(1, 1000).When(x => x.Hours.HasValue);
    }
}
