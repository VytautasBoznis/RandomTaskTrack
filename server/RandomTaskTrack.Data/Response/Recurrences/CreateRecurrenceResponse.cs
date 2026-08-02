using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recurrences;

public class CreateRecurrenceResponse : BaseResponse
{
    public RecurrenceListItemDto Recurrence { get; set; } = new();
    public int MaterializedTaskCount { get; set; }
}
