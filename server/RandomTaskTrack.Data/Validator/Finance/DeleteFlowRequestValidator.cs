using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class DeleteFlowRequestValidator : AbstractValidator<DeleteFlowRequest>
{
    public DeleteFlowRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
