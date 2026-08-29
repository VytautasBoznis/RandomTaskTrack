using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Finance;

public class GetProjectionOperation : BaseOperation<GetProjectionRequest, GetProjectionResponse>
{
    private readonly IFinanceProjector _projector;

    public GetProjectionOperation(
        ILogger<GetProjectionOperation> logger,
        IValidator<GetProjectionRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceProjector projector) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _projector = projector;
    }

    protected override async Task<GetProjectionResponse> Execute(GetProjectionRequest request, IUnitOfWork unitOfWork)
    {
        return new GetProjectionResponse
        {
            Points = await _projector.ProjectAsync(
                request.HistoryMonths,
                request.Months,
                request.StockGrowthPct,
                unitOfWork)
        };
    }
}
