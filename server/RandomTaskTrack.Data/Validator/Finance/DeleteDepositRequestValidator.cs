using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteDepositRequestValidator : AbstractValidator<DeleteDepositRequest>
{
    public DeleteDepositRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
