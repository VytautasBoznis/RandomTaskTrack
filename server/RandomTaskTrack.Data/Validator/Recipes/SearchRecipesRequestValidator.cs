using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class SearchRecipesRequestValidator : AbstractValidator<SearchRecipesRequest>
{
    public SearchRecipesRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(200);

        // Upper bound because every result costs source quota.
        RuleFor(x => x.Number).InclusiveBetween(1, 25).When(x => x.Number.HasValue);
    }
}
