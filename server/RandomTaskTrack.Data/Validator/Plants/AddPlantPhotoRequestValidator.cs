using FluentValidation;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class AddPlantPhotoRequestValidator : AbstractValidator<AddPlantPhotoRequest>
{
    public AddPlantPhotoRequestValidator()
    {
        RuleFor(x => x.PlantId).NotEmpty();
        RuleFor(x => x.ImageBase64).NotEmpty().MaximumLength(ImageMediaTypes.MaxBase64Length);

        RuleFor(x => x.MediaType)
            .Must(type => ImageMediaTypes.All.Contains(type))
            .WithMessage($"Must be one of {string.Join(", ", ImageMediaTypes.All)}.");

        RuleFor(x => x.Stage).MaximumLength(200);
    }
}
