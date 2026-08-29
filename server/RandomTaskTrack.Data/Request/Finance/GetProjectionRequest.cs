using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class GetProjectionRequest : AuthenticatedRequest
{
    /// <summary>Months forward from this month. Five years by default.</summary>
    public int Months { get; set; } = 60;

    /// <summary>Months of ledger history to include behind today.</summary>
    public int HistoryMonths { get; set; } = 12;

    /// <summary>
    /// Assumed annual return on holdings, as a percentage. Zero means hold at
    /// the last pulled price — a projection needs an assumption, but an honest
    /// default beats a flattering one.
    /// </summary>
    public decimal StockGrowthPct { get; set; }
}
