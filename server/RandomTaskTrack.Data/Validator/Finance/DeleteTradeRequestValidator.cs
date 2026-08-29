using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteTradeRequestValidator : AbstractValidator<DeleteTradeRequest>
{
    public DeleteTradeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
