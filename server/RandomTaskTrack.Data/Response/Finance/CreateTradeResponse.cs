using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class CreateTradeResponse : BaseResponse
{
    public Trade Trade { get; set; } = new();
}
