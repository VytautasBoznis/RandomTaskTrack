using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class DraftLearningPlanRequestValidator : AbstractValidator<DraftLearningPlanRequest>
{
    public DraftLearningPlanRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Context).MaximumLength(4000);
    }
}
