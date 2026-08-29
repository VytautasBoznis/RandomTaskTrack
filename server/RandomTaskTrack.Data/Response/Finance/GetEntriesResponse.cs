using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class GetEntriesResponse : BaseResponse
{
    public List<LedgerEntry> Entries { get; set; } = new();
}
