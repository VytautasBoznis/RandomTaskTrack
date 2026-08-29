using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class DeletePlantRequestValidator : AbstractValidator<DeletePlantRequest>
{
    public DeletePlantRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
