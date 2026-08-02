using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class GetTasksOperation : BaseOperation<GetTasksRequest, GetTasksResponse>
{
    private readonly ITasksRepository _tasksRepository;

    public GetTasksOperation(
        ILogger<GetTasksOperation> logger,
        IValidator<GetTasksRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
    }

    protected override async Task<GetTasksResponse> Execute(GetTasksRequest request, IUnitOfWork unitOfWork)
    {
        return new GetTasksResponse
        {
            Tasks = await _tasksRepository.QueryAsync(
                request.DomainId, request.FromDate, request.ToDate, request.Status,
                request.Search, request.Page, request.PageSize, unitOfWork),
            TotalCount = await _tasksRepository.CountAsync(
                request.DomainId, request.FromDate, request.ToDate, request.Status,
                request.Search, unitOfWork)
        };
    }
}
