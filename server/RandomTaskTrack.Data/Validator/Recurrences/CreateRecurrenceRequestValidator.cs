using FluentValidation;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Recurrences;

namespace RandomTaskTrack.Data.Validator.Recurrences;

public class CreateRecurrenceRequestValidator : AbstractValidator<CreateRecurrenceRequest>
{
    public CreateRecurrenceRequestValidator()
    {
        RuleFor(x => x.DomainId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Data).Must(JsonRules.BeAJsonObjectOrNull).WithMessage("Data must be a JSON object.");
        RuleFor(x => x.RuleType).IsInEnum();
        RuleFor(x => x.AnchorMode).IsInEnum();

        RuleFor(x => x.EndsOn)
            .GreaterThanOrEqualTo(x => x.StartsOn!.Value)
            .When(x => x.StartsOn.HasValue && x.EndsOn.HasValue)
            .WithMessage("EndsOn must be on or after StartsOn.");

        // Mirrors ck_task_recurrences_shape in the schema. Kept in both places
        // deliberately: the DB constraint is the guarantee, this is the
        // readable error.
        RuleFor(x => x.IntervalDays)
            .NotNull().GreaterThan(0)
            .When(x => x.RuleType == RecurrenceRuleType.IntervalDays)
            .WithMessage("IntervalDays must be a positive number for an interval recurrence.");

        RuleFor(x => x.DaysOfWeek)
            .NotNull().Must(d => d != null && d.Length > 0)
            .When(x => x.RuleType == RecurrenceRuleType.DaysOfWeek)
            .WithMessage("DaysOfWeek must contain at least one day for a weekly recurrence.");

        RuleForEach(x => x.DaysOfWeek)
            .InclusiveBetween(0, 6)
            .When(x => x.DaysOfWeek != null)
            .WithMessage("DaysOfWeek entries must be 0 (Sunday) through 6 (Saturday).");

        RuleFor(x => x.DayOfMonth)
            .NotNull().InclusiveBetween(1, 31)
            .When(x => x.RuleType == RecurrenceRuleType.DayOfMonth)
            .WithMessage("DayOfMonth must be between 1 and 31 for a monthly recurrence.");
    }
}
