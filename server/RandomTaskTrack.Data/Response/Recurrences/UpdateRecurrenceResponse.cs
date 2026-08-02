using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recurrences;

public class UpdateRecurrenceResponse : BaseResponse
{
    public RecurrenceListItemDto Recurrence { get; set; } = new();
}
