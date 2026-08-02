using FluentValidation;
using RandomTaskTrack.Data.Request.Chat;

namespace RandomTaskTrack.Data.Validator.Chat;

public class GetConversationRequestValidator : AbstractValidator<GetConversationRequest>
{
    public GetConversationRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
