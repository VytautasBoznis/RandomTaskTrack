using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class GetEntriesRequestValidator : AbstractValidator<GetEntriesRequest>
{
    public GetEntriesRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().When(x => x.Kind.HasValue);
        RuleFor(x => x.Limit).InclusiveBetween(1, 1000);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From!.Value).When(x => x.From.HasValue && x.To.HasValue);
    }
}
