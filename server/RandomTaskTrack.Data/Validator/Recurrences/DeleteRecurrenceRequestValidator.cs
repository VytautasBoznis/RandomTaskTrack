using FluentValidation;
using RandomTaskTrack.Data.Request.Recurrences;

namespace RandomTaskTrack.Data.Validator.Recurrences;

public class DeleteRecurrenceRequestValidator : AbstractValidator<DeleteRecurrenceRequest>
{
    public DeleteRecurrenceRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
