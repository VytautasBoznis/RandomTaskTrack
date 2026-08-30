using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Response.Base;

namespace RandomTaskTrack.Data.Response.Finance;

public class UpdateAccountResponse : BaseResponse
{
    public FinanceAccount Account { get; set; } = new();
}
