using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateDepositRequestValidator : AbstractValidator<CreateDepositRequest>
{
    public CreateDepositRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);

        // A percentage, not a fraction. 100 is already generous for a deposit;
        // anything above it is far likelier to be 0.04 typed as 4 then 400.
        RuleFor(x => x.AnnualRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Compounding).IsInEnum().When(x => x.Compounding.HasValue);
        RuleFor(x => x.OpenedOn).NotEmpty();
        RuleFor(x => x.MaturesOn).GreaterThanOrEqualTo(x => x.OpenedOn).When(x => x.MaturesOn.HasValue);
    }
}
