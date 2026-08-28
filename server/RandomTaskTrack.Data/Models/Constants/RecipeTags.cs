namespace RandomTaskTrack.Data.Models.Constants;

/// <summary>
/// Tags the app itself gives meaning to. They are ordinary rows in
/// recipe_recipes.tags, so they show as chips, survive an edit, and are found by
/// the same tag filter as any other tag — typing one by hand does exactly what
/// the button does.
/// </summary>
public static class RecipeTags
{
    /// <summary>
    /// Keeps a dish out of the rotation for good. Banking a whole pull means
    /// dishes land in the library that nobody chose, and rerolling only rejects
    /// one for the current week — without this, a dish you never want keeps
    /// coming back. Naming it as a tag rather than a column is what makes the
    /// skipped ones searchable instead of hidden.
    /// </summary>
    public const string NotPicked = "not picked";
}
