namespace RandomTaskTrack.Data.Dtos.Finance;

/// <summary>
/// One month of the projection, everything converted to base. Monthly buckets
/// because daily over ten years is 3,650 points nobody can read.
/// </summary>
public class ProjectionPointDto
{
    /// <summary>The first of the month this bucket covers.</summary>
    public DateOnly Month { get; set; }

    /// <summary>
    /// True for months at or before today, where income and expenses come from
    /// the ledger rather than from the flow definitions. Net worth is only
    /// populated forward — valuing holdings in the past would need historical
    /// prices this app does not store.
    /// </summary>
    public bool IsActual { get; set; }

    public decimal Income { get; set; }
    public decimal Expenses { get; set; }

    /// <summary>Income − Expenses for the month.</summary>
    public decimal Net { get; set; }

    /// <summary>Null on actual months — see <see cref="IsActual"/>.</summary>
    public decimal? Cash { get; set; }

    public decimal? Deposits { get; set; }
    public decimal? Stocks { get; set; }
    public decimal? NetWorth { get; set; }
}
