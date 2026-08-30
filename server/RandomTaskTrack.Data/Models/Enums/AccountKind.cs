namespace RandomTaskTrack.Data.Models.Enums;

/// <summary>
/// What an account is for. This does not change the arithmetic — every account
/// carries a cash balance and every account can hold shares — it decides which
/// accounts the holding form offers and how the card reads.
/// </summary>
public enum AccountKind : int
{
    /// <summary>A bank account: current, savings, whatever holds cash.</summary>
    Cash = 1,

    /// <summary>A brokerage or pension: worth what the shares in it are worth.</summary>
    Stock = 2
}
