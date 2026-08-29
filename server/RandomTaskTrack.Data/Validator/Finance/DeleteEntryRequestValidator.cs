using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteEntryRequestValidator : AbstractValidator<DeleteEntryRequest>
{
    public DeleteEntryRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
