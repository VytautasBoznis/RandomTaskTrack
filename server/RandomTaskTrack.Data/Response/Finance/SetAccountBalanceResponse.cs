using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class SetAccountBalanceResponse : BaseResponse
{
    /// <summary>
    /// The adjustment that was written, or null when the balance already read
    /// what was asked for — nothing moved, so nothing is logged.
    /// </summary>
    public LedgerEntry? Entry { get; set; }
}
