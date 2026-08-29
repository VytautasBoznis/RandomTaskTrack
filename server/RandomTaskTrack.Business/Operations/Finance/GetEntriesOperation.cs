using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class GetEntriesOperation : BaseOperation<GetEntriesRequest, GetEntriesResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public GetEntriesOperation(
        ILogger<GetEntriesOperation> logger,
        IValidator<GetEntriesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override async Task<GetEntriesResponse> Execute(GetEntriesRequest request, IUnitOfWork unitOfWork)
    {
        return new GetEntriesResponse
        {
            Entries = await _financeRepository.QueryEntriesAsync(
                request.From,
                request.To,
                request.Kind,
                string.IsNullOrWhiteSpace(request.Search) ? null : request.Search,
                request.Limit,
                unitOfWork)
        };
    }
}
