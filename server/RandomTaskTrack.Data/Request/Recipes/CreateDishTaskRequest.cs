using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

public class CreateDishTaskRequest : AuthenticatedRequest
{
    public Guid PickId { get; set; }

    /// <summary>Null puts it on the last day of the dish's week.</summary>
    public DateOnly? DueOn { get; set; }
}
