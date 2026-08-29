using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateDividendRequestValidator : AbstractValidator<UpdateDividendRequest>
{
    public UpdateDividendRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Cadence).IsInEnum().When(x => x.Cadence.HasValue);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 31).When(x => x.DayOfMonth.HasValue);
        RuleFor(x => x.MonthOfYear).InclusiveBetween(1, 12).When(x => x.MonthOfYear.HasValue);
    }
}
