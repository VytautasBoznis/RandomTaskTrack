using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteTargetRequestValidator : AbstractValidator<DeleteTargetRequest>
{
    public DeleteTargetRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
