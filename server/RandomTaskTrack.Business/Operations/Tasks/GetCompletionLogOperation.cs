using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class GetCompletionLogOperation : BaseOperation<GetCompletionLogRequest, GetCompletionLogResponse>
{
    private readonly ICompletionsRepository _completionsRepository;

    public GetCompletionLogOperation(
        ILogger<GetCompletionLogOperation> logger,
        IValidator<GetCompletionLogRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ICompletionsRepository completionsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _completionsRepository = completionsRepository;
    }

    protected override async Task<GetCompletionLogResponse> Execute(GetCompletionLogRequest request, IUnitOfWork unitOfWork)
    {
        return new GetCompletionLogResponse
        {
            Entries = await _completionsRepository.QueryAsync(
                request.DomainId, request.TitleContains, request.FromDate,
                request.ToDate, request.Limit, unitOfWork)
        };
    }
}
