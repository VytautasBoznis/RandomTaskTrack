using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteDividendRequestValidator : AbstractValidator<DeleteDividendRequest>
{
    public DeleteDividendRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
