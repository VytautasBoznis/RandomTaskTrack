using FluentValidation;
using RandomTaskTrack.Data.Request.Chat;

namespace RandomTaskTrack.Data.Validator.Chat;

public class DeleteConversationRequestValidator : AbstractValidator<DeleteConversationRequest>
{
    public DeleteConversationRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
