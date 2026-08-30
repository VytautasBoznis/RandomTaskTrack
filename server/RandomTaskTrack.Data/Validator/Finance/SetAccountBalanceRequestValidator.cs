using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class SetAccountBalanceRequestValidator : AbstractValidator<SetAccountBalanceRequest>
{
    public SetAccountBalanceRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        // No bound on Balance beyond the column's: it can be negative, and a
        // large number here is a large account, not a typo we can detect.
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
