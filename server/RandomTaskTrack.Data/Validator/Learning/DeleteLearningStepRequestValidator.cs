using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class DeleteLearningStepRequestValidator : AbstractValidator<DeleteLearningStepRequest>
{
    public DeleteLearningStepRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
