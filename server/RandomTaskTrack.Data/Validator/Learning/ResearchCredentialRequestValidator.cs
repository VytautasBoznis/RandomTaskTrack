using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class ResearchCredentialRequestValidator : AbstractValidator<ResearchCredentialRequest>
{
    public ResearchCredentialRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
