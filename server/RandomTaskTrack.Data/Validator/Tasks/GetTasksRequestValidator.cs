using FluentValidation;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class GetTasksRequestValidator : AbstractValidator<GetTasksRequest>
{
    public GetTasksRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be on or after FromDate.");
    }
}
