using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recurrences;

public class DeleteRecurrenceResponse : BaseResponse
{
    public bool Success { get; set; }
    public int DeletedTaskCount { get; set; }
}
