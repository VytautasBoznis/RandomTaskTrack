using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class GetDashboardRequestValidator : AbstractValidator<GetDashboardRequest>
{
    public GetDashboardRequestValidator()
    {
        RuleFor(x => x.UpcomingDays).InclusiveBetween(1, 90);
    }
}
