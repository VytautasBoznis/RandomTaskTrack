using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class GetCompletionLogRequestValidator : AbstractValidator<GetCompletionLogRequest>
{
    public GetCompletionLogRequestValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 1000);
    }
}
