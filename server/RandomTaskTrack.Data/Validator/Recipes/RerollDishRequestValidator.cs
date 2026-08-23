using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class RerollDishRequestValidator : AbstractValidator<RerollDishRequest>
{
    public RerollDishRequestValidator()
    {
        RuleFor(x => x.FamilyId).GreaterThan(0).When(x => x.FamilyId.HasValue);
    }
}
