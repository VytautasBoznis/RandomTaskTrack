using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
