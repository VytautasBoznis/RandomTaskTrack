using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class UpdateTradeRequestValidator : AbstractValidator<UpdateTradeRequest>
{
    public UpdateTradeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Side).IsInEnum().When(x => x.Side.HasValue);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Fee).GreaterThanOrEqualTo(0).When(x => x.Fee.HasValue);
    }
}
