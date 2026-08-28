using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class UpdateRecipeRequestValidator : AbstractValidator<UpdateRecipeRequest>
{
    public UpdateRecipeRequestValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();

        // Matches ck_recipe_recipes_rating, so a bad rating is a 400 rather than
        // a constraint violation surfacing as a 500.
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);

        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(50).When(x => x.Tags != null);
    }
}
