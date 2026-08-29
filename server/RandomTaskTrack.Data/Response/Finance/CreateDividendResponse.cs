using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateDividendResponse : BaseResponse
{
    public Dividend Dividend { get; set; } = new();
}
