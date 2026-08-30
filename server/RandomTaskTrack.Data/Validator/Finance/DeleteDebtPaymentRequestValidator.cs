using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteDebtPaymentRequestValidator : AbstractValidator<DeleteDebtPaymentRequest>
{
    public DeleteDebtPaymentRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
