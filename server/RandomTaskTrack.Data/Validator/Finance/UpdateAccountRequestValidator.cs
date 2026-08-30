using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Kind).IsInEnum().When(x => x.Kind.HasValue);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
