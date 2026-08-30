using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class DeleteLearningGoalRequestValidator : AbstractValidator<DeleteLearningGoalRequest>
{
    public DeleteLearningGoalRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
