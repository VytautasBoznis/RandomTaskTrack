using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteDebtRequestValidator : AbstractValidator<DeleteDebtRequest>
{
    public DeleteDebtRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
