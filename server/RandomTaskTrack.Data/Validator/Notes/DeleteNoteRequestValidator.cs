using FluentValidation;
using RandomTaskTrack.Data.Request.Notes;

namespace RandomTaskTrack.Data.Validator.Notes;

public class DeleteNoteRequestValidator : AbstractValidator<DeleteNoteRequest>
{
    public DeleteNoteRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}