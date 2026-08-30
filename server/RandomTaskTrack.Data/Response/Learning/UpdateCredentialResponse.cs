using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Learning;

public class UpdateCredentialResponse : BaseResponse
{
    public LearningCredentialDto Credential { get; set; } = new();
}
