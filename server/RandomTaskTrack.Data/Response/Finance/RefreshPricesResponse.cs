using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class RefreshPricesResponse : BaseResponse
{
    public int UpdatedHoldings { get; set; }
    public int UpdatedCurrencies { get; set; }

    /// <summary>
    /// Symbols the source had no price for. Reported rather than thrown: one
    /// dead ticker should not cost you the other nine prices.
    /// </summary>
    public List<string> Failed { get; set; } = new();
}
