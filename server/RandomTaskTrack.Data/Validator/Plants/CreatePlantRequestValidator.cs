using FluentValidation;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Request.Plants;

namespace RandomTaskTrack.Data.Validator.Plants;

public class CreatePlantRequestValidator : AbstractValidator<CreatePlantRequest>
{
    public CreatePlantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);

        // Long enough for a paragraph about the leaves, short enough that the
        // lookup prompt stays a lookup prompt.
        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.ImageBase64).MaximumLength(ImageMediaTypes.MaxBase64Length);

        // Only checked when there is actually a photo — most plants are added
        // by description alone.
        RuleFor(x => x.MediaType)
            .Must(type => type is not null && ImageMediaTypes.All.Contains(type))
            .WithMessage($"Must be one of {string.Join(", ", ImageMediaTypes.All)}.")
            .When(x => !string.IsNullOrEmpty(x.ImageBase64));
    }
}
