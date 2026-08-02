using FluentValidation;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.Data.Validator.Tasks;

public class CompleteTaskRequestValidator : AbstractValidator<CompleteTaskRequest>
{
    public CompleteTaskRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => s is TaskItemStatus.Done or TaskItemStatus.Skipped)
            .WithMessage("Status must be Done or Skipped.");
        RuleFor(x => x.ActualData).Must(JsonRules.BeAJsonObjectOrNull).WithMessage("ActualData must be a JSON object.");
    }
}
