using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Domains;

public class GetDomainsResponse : BaseResponse
{
    public List<TaskDomain> Domains { get; set; } = new();
}
