using FluentValidation;
using RandomTaskTrack.Data.Request.Chat;

namespace RandomTaskTrack.Data.Validator.Chat;

public class GetConversationsRequestValidator : AbstractValidator<GetConversationsRequest>
{
    public GetConversationsRequestValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}
