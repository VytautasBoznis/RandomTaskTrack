using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class ResearchPlantRequestValidator : AbstractValidator<ResearchPlantRequest>
{
    public ResearchPlantRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
