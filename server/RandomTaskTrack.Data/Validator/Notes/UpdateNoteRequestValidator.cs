using FluentValidation;
using RandomTaskTrack.Data.Request.Notes;

namespace RandomTaskTrack.Data.Validator.Notes;

public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        // Null means "leave alone"; an explicit empty title would leave the note
        // unlabelled in the list, so it is rejected rather than stored.
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500).When(x => x.Title != null);
    }
}