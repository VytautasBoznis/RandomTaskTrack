using FluentValidation;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class GetPlantPhotoRequestValidator : AbstractValidator<GetPlantPhotoRequest>
{
    public GetPlantPhotoRequestValidator()
    {
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}
