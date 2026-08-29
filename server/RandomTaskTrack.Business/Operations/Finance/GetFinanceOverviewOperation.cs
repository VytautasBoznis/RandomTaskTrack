using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Finance;

/// <summary>
/// The whole tab in one round trip, as GetDashboardOperation is for tasks.
/// </summary>
public class GetFinanceOverviewOperation : BaseOperation<GetFinanceOverviewRequest, GetFinanceOverviewResponse>
{
    private readonly IFinanceProjector _projector;

    public GetFinanceOverviewOperation(
        ILogger<GetFinanceOverviewOperation> logger,
        IValidator<GetFinanceOverviewRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceProjector projector) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _projector = projector;
    }

    protected override async Task<GetFinanceOverviewResponse> Execute(GetFinanceOverviewRequest request, IUnitOfWork unitOfWork)
    {
        return new GetFinanceOverviewResponse
        {
            Overview = await _projector.BuildOverviewAsync(unitOfWork)
        };
    }
}
