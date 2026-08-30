using RandomTaskTrack.Data.Request.Base;

namespace RandomTaskTrack.Data.Request.Finance;

public class DeleteDebtPaymentRequest : AuthenticatedRequest
{
    public Guid Id { get; set; }
}
