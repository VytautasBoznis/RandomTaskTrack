using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title != null);
        RuleFor(x => x.Data).Must(JsonRules.BeAJsonObjectOrNull).WithMessage("Data must be a JSON object.");
    }
}
