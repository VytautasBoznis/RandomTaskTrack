using FluentValidation;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.Data.Validator.Learning;

public class UpdateCredentialRequestValidator : AbstractValidator<UpdateCredentialRequest>
{
    public UpdateCredentialRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Issuer).MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.CredentialId).MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);

        RuleFor(x => x.EarnedOn).NotEmpty();
        RuleFor(x => x.RenewalKind).IsInEnum();

        RuleFor(x => x)
            .Must(x => CredentialRules.BeAConsistentExpiry(x.RenewalKind, x.ExpiresOn))
            .WithName(nameof(UpdateCredentialRequest.ExpiresOn))
            .WithMessage(CredentialRules.ExpiryMessage);
    }
}
