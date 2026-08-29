using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateDepositRequestValidator : AbstractValidator<UpdateDepositRequest>
{
    public UpdateDepositRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Principal).GreaterThan(0).When(x => x.Principal.HasValue);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.AnnualRate).InclusiveBetween(0, 100).When(x => x.AnnualRate.HasValue);
        RuleFor(x => x.Compounding).IsInEnum().When(x => x.Compounding.HasValue);
    }
}
