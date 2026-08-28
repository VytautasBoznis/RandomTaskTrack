using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class SaveRecipesRequestValidator : AbstractValidator<SaveRecipesRequest>
{
    public SaveRecipesRequestValidator()
    {
        RuleFor(x => x.Recipes).NotEmpty();

        RuleForEach(x => x.Recipes).ChildRules(recipe =>
        {
            recipe.RuleFor(r => r.ExternalId).NotEmpty().MaximumLength(200);
            recipe.RuleFor(r => r.Title).NotEmpty().MaximumLength(500);
        });
    }
}
