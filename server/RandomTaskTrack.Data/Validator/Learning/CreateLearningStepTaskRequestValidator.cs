using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class CreateLearningStepTaskRequestValidator : AbstractValidator<CreateLearningStepTaskRequest>
{
    public CreateLearningStepTaskRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
