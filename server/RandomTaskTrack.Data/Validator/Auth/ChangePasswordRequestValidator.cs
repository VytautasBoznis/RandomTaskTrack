using FluentValidation;
using RandomTaskTrack.Data.Request.Auth;

namespace RandomTaskTrack.Data.Validator.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current one.");
    }
}
