using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
