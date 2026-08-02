using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.DomainId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DueOn).NotEqual(default(DateOnly)).WithMessage("DueOn is required.");
        RuleFor(x => x.Data).Must(JsonRules.BeAJsonObjectOrNull).WithMessage("Data must be a JSON object.");
    }
}
