namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// Which way the money moves. Amounts are always stored positive; this carries
/// the sign.
/// </summary>
public enum FinanceFlowKind : int
{
    Income = 1,
    Expense = 2
}
