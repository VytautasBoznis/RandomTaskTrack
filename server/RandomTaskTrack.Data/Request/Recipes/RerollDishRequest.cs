using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Recipes;

public class RerollDishRequest : AuthenticatedRequest
{
    /// <summary>Null keeps the rotation's choice — the family that has gone
    /// longest without a dish.</summary>
    public int? FamilyId { get; set; }
}
