using FluentValidation;
using RandomTaskTrack.Data.Request.Auth;

namespace RandomTaskTrack.Data.Validator.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email address.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
