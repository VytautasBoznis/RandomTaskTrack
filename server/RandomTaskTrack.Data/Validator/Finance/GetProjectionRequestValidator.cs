using FluentValidation;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.Data.Validator.Finance;

public class GetProjectionRequestValidator : AbstractValidator<GetProjectionRequest>
{
    public GetProjectionRequestValidator()
    {
        // 600 months is 50 years. Past that the projection is fiction and the
        // series is big enough to be worth not building by accident.
        RuleFor(x => x.Months).InclusiveBetween(1, 600);
        RuleFor(x => x.HistoryMonths).InclusiveBetween(0, 600);
        RuleFor(x => x.StockGrowthPct).InclusiveBetween(-100, 100);
    }
}
