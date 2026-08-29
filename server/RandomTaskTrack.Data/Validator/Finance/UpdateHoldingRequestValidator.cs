using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateHoldingRequestValidator : AbstractValidator<UpdateHoldingRequest>
{
    public UpdateHoldingRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(40).When(x => x.Symbol is not null);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
    }
}
