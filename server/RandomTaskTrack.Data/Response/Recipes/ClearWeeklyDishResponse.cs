using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recipes;

public class ClearWeeklyDishResponse : BaseResponse
{
    /// <summary>False when there was nothing to clear, so clicking twice is
    /// honest rather than pretending to have done something.</summary>
    public bool Cleared { get; set; }
}
