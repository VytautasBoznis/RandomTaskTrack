using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateHoldingRequestValidator : AbstractValidator<CreateHoldingRequest>
{
    public CreateHoldingRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
