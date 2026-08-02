using FluentValidation;
using RandomTaskTrack.Data.Request.Chat;

namespace RandomTaskTrack.Data.Validator.Chat;

public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(20000);
    }
}
