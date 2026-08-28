using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>Takes this week's dish off the board without choosing another.</summary>
public class ClearWeeklyDishRequest : AuthenticatedRequest
{
}
