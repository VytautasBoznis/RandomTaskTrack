using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateDebtPaymentRequestValidator : AbstractValidator<CreateDebtPaymentRequest>
{
    public CreateDebtPaymentRequestValidator()
    {
        RuleFor(x => x.DebtId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaidOn).NotEmpty();
    }
}
