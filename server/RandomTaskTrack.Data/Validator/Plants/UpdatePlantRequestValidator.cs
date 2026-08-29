using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class UpdatePlantRequestValidator : AbstractValidator<UpdatePlantRequest>
{
    public UpdatePlantRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        // Null leaves the name alone; an explicit blank one would leave the
        // plant unlabelled in the list, so it is rejected rather than stored.
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name != null);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Species).MaximumLength(200);
        RuleFor(x => x.LatinName).MaximumLength(200);
    }
}
