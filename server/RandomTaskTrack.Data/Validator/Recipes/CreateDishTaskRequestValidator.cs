using FluentValidation;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.Data.Validator.Recipes;

public class CreateDishTaskRequestValidator : AbstractValidator<CreateDishTaskRequest>
{
    public CreateDishTaskRequestValidator()
    {
        RuleFor(x => x.PickId).NotEmpty();
    }
}
