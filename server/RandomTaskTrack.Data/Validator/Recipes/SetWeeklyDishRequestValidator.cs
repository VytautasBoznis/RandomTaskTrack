using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class SetWeeklyDishRequestValidator : AbstractValidator<SetWeeklyDishRequest>
{
    public SetWeeklyDishRequestValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
    }
}
