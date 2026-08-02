using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class DeleteTaskRequestValidator : AbstractValidator<DeleteTaskRequest>
{
    public DeleteTaskRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
