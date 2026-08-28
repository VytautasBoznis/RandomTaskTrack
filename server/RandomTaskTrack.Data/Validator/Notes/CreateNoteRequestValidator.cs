using FluentValidation;
using RandomTaskTrack.Data.Request.Notes;

namespace RandomTaskTrack.Data.Validator.Notes;

public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}