using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class CreateTargetRequestValidator : AbstractValidator<CreateTargetRequest>
{
    public CreateTargetRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);

        // Mirrors ck_fin_targets_something: a target with neither a date nor an
        // amount has nothing to draw.
        RuleFor(x => x.TargetOn)
            .Must((request, _) => request.TargetOn.HasValue || request.Amount.HasValue)
            .WithMessage("A target needs a date, an amount, or both.");
    }
}
