using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class UpdateLearningGoalRequestValidator : AbstractValidator<UpdateLearningGoalRequest>
{
    public UpdateLearningGoalRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tier).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Why).MaximumLength(2000);
        RuleFor(x => x.Benefits).MaximumLength(2000);
        RuleFor(x => x.Context).MaximumLength(4000);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
