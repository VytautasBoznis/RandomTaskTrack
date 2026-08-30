using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class CreateRenewalReminderRequestValidator : AbstractValidator<CreateRenewalReminderRequest>
{
    public CreateRenewalReminderRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
