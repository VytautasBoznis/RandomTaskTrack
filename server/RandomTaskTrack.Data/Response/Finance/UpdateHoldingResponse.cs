using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class UpdateHoldingResponse : BaseResponse
{
    public Holding Holding { get; set; } = new();
}
