using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Recurrences;

public class GetRecurrencesResponse : BaseResponse
{
    public List<RecurrenceListItemDto> Recurrences { get; set; } = new();
}
