using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

/// <summary>Promotes a library dish to this week's, bypassing the rotation.</summary>
public class SetWeeklyDishRequest : AuthenticatedRequest
{
    public Guid RecipeId { get; set; }
}
