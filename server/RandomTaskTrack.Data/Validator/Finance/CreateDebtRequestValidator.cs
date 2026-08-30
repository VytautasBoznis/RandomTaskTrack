using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateDebtRequestValidator : AbstractValidator<CreateDebtRequest>
{
    public CreateDebtRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);

        // A percentage, not a fraction, and the same ceiling the deposits use:
        // above 100 it is far likelier to be 0.03 typed as 3 then 300.
        RuleFor(x => x.AnnualRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Payment).GreaterThan(0);
        RuleFor(x => x.StartsOn).NotEmpty();
        RuleFor(x => x.EndsOn).GreaterThanOrEqualTo(x => x.StartsOn).When(x => x.EndsOn.HasValue);
        RuleFor(x => x.AssetValue).GreaterThanOrEqualTo(0).When(x => x.AssetValue.HasValue);
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0).When(x => x.DownPayment.HasValue);

        RuleFor(x => x.DownPayment)
            .NotNull()
            .When(x => x.DownPaymentAccountId.HasValue)
            .WithMessage("Say how much the downpayment is if it comes out of an account.");

        // The rule that actually matters — a payment too small to cover the
        // interest — is not here. It needs the principal and the rate together,
        // and on update those can come half from the request and half from the
        // stored row, so it lives in FinanceGuards.GuardAmortises and runs on
        // the merged debt in both operations rather than twice, differently.
    }
}
