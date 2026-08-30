using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateDebtRequestValidator : AbstractValidator<UpdateDebtRequest>
{
    public UpdateDebtRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Principal).GreaterThan(0).When(x => x.Principal.HasValue);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.AnnualRate).InclusiveBetween(0, 100).When(x => x.AnnualRate.HasValue);
        RuleFor(x => x.Payment).GreaterThan(0).When(x => x.Payment.HasValue);
        RuleFor(x => x.AssetValue).GreaterThanOrEqualTo(0).When(x => x.AssetValue.HasValue);
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0).When(x => x.DownPayment.HasValue);

        // StartsOn against EndsOn is not checked here: either can arrive alone,
        // with the other coming from the stored row, so the comparison only
        // means anything after the merge. FinanceGuards.GuardAmortises does it.
    }
}
