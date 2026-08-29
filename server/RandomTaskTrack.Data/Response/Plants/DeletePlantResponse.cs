using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Plants;

public class DeletePlantResponse : BaseResponse
{
    public bool Success { get; set; }

    /// <summary>Care schedules removed with the plant.</summary>
    public int DeletedRecurrenceCount { get; set; }

    /// <summary>Pending tasks removed with it. Completed history is kept.</summary>
    public int DeletedTaskCount { get; set; }
}
