namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>
/// A mark on the projection. Both columns are nullable on purpose, which gives
/// three useful shapes: an amount alone is a goal line, a date alone is a
/// milestone, and both together is a point to hit — "100k by 2030".
/// </summary>
public class FinanceTarget
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public DateOnly? TargetOn { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
