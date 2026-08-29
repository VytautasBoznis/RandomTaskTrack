using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteHoldingRequestValidator : AbstractValidator<DeleteHoldingRequest>
{
    public DeleteHoldingRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
