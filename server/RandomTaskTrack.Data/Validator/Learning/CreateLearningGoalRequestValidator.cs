using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class CreateLearningGoalRequestValidator : AbstractValidator<CreateLearningGoalRequest>
{
    public CreateLearningGoalRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tier).IsInEnum();

        // Long enough for a paragraph of motivation, which is the point of the
        // field, and short enough that the draft prompt stays a prompt.
        RuleFor(x => x.Why).MaximumLength(2000);
        RuleFor(x => x.Benefits).MaximumLength(2000);
        RuleFor(x => x.Context).MaximumLength(4000);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
