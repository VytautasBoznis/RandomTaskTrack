using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Recipes;

public class RecipePick
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }

    /// <summary>Monday of the ISO week, in the scheduler's timezone.</summary>
    public DateOnly WeekOf { get; set; }

    public RecipePickStatus Status { get; set; }
    public Guid? TaskId { get; set; }
    public DateTime CreatedAt { get; set; }
}
