using FluentValidation;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Recurrences;

namespace RandomTaskTrack.Data.Validator.Recurrences;

public class UpdateRecurrenceRequestValidator : AbstractValidator<UpdateRecurrenceRequest>
{
    public UpdateRecurrenceRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title != null);
        RuleFor(x => x.Data).Must(JsonRules.BeAJsonObjectOrNull).WithMessage("Data must be a JSON object.");
        RuleFor(x => x.RuleType).IsInEnum().When(x => x.RuleType.HasValue);
        RuleFor(x => x.AnchorMode).IsInEnum().When(x => x.AnchorMode.HasValue);

        RuleFor(x => x.IntervalDays).GreaterThan(0).When(x => x.IntervalDays.HasValue);
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 31).When(x => x.DayOfMonth.HasValue);
        RuleForEach(x => x.DaysOfWeek).InclusiveBetween(0, 6).When(x => x.DaysOfWeek != null);

        // A rule-type switch has to bring its own shape with it, otherwise the
        // row would fail the DB check constraint using stale columns.
        RuleFor(x => x.IntervalDays)
            .NotNull()
            .When(x => x.RuleType == RecurrenceRuleType.IntervalDays)
            .WithMessage("Switching to an interval recurrence requires IntervalDays.");

        RuleFor(x => x.DaysOfWeek)
            .NotNull().Must(d => d != null && d.Length > 0)
            .When(x => x.RuleType == RecurrenceRuleType.DaysOfWeek)
            .WithMessage("Switching to a weekly recurrence requires DaysOfWeek.");

        RuleFor(x => x.DayOfMonth)
            .NotNull()
            .When(x => x.RuleType == RecurrenceRuleType.DayOfMonth)
            .WithMessage("Switching to a monthly recurrence requires DayOfMonth.");
    }
}
