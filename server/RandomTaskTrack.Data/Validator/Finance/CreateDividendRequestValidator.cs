using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateDividendRequestValidator : AbstractValidator<CreateDividendRequest>
{
    public CreateDividendRequestValidator()
    {
        RuleFor(x => x.Cadence).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.StartsOn).NotEmpty();
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 31).When(x => x.DayOfMonth.HasValue);
        RuleFor(x => x.MonthOfYear).InclusiveBetween(1, 12).When(x => x.MonthOfYear.HasValue);
        RuleFor(x => x.EndsOn).GreaterThanOrEqualTo(x => x.StartsOn).When(x => x.EndsOn.HasValue);
    }
}
