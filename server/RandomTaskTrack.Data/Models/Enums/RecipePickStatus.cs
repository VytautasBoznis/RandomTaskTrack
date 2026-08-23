namespace RandomTaskTrack.Data.Models.Enums;

public enum RecipePickStatus : int
{
    Current = 1,

    /// <summary>Superseded by a reroll. Kept so the dish is not offered again.</summary>
    Rerolled = 2
}
