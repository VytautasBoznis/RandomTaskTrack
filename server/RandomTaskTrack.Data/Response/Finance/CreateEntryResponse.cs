using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateEntryResponse : BaseResponse
{
    public LedgerEntry Entry { get; set; } = new();
}
