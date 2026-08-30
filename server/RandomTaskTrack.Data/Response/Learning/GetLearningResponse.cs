using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

/// <summary>The whole tab in one round trip: every path with its steps, and
/// every credential held.</summary>
public class GetLearningResponse : BaseResponse
{
    public List<LearningGoalDto> Goals { get; set; } = new();
    public List<LearningCredentialDto> Credentials { get; set; } = new();
}
