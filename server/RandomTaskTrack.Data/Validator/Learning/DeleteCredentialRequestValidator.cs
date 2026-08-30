using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class DeleteCredentialRequestValidator : AbstractValidator<DeleteCredentialRequest>
{
    public DeleteCredentialRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
