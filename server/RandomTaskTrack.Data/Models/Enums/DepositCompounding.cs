namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// How a deposit's interest accrues. Unlike a share price this is contractual,
/// so the projection values deposits exactly rather than assuming.
/// </summary>
public enum DepositCompounding : int
{
    /// <summary>principal × (1 + rate × years)</summary>
    Simple = 1,

    /// <summary>principal × (1 + rate/12)^months</summary>
    Monthly = 2,

    /// <summary>principal × (1 + rate)^years</summary>
    Annual = 3
}
