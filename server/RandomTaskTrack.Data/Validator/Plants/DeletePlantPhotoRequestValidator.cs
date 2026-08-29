using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class DeletePlantPhotoRequestValidator : AbstractValidator<DeletePlantPhotoRequest>
{
    public DeletePlantPhotoRequestValidator()
    {
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}
