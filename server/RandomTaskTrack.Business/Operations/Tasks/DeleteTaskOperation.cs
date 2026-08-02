using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class DeleteTaskOperation : BaseOperation<DeleteTaskRequest, DeleteTaskResponse>
{
    private readonly ITasksRepository _tasksRepository;

    public DeleteTaskOperation(
        ILogger<DeleteTaskOperation> logger,
        IValidator<DeleteTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteTaskResponse> Execute(DeleteTaskRequest request, IUnitOfWork unitOfWork)
    {
        bool deleted = await _tasksRepository.DeleteAsync(request.Id, unitOfWork);

        if (!deleted)
        {
            throw new NotFoundException($"No task with id {request.Id}", ExceptionCodes.TASK_NOT_FOUND);
        }

        return new DeleteTaskResponse { Success = true };
    }
}
