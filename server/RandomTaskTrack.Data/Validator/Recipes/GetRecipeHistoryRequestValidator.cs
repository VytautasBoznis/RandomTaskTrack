using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class GetRecipeHistoryRequestValidator : AbstractValidator<GetRecipeHistoryRequest>
{
    public GetRecipeHistoryRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search != null);
    }
}
