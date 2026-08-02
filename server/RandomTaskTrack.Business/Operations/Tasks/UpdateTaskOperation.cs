using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class UpdateTaskOperation : BaseOperation<UpdateTaskRequest, UpdateTaskResponse>
{
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;

    public UpdateTaskOperation(
        ILogger<UpdateTaskOperation> logger,
        IValidator<UpdateTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateTaskResponse> Execute(UpdateTaskRequest request, IUnitOfWork unitOfWork)
    {
        TaskItem task = await _tasksRepository.GetRawByIdAsync(request.Id, unitOfWork)
                        ?? throw new NotFoundException($"No task with id {request.Id}", ExceptionCodes.TASK_NOT_FOUND);

        if (request.DomainId.HasValue && request.DomainId.Value != task.DomainId)
        {
            _ = await _domainsRepository.GetByIdAsync(request.DomainId.Value, unitOfWork)
                ?? throw new NotFoundException($"No domain with id {request.DomainId}", ExceptionCodes.DOMAIN_NOT_FOUND);

            task.DomainId = request.DomainId.Value;
        }

        // Null means "leave alone", so a partial update never blanks a field
        // the caller simply did not mention.
        task.Title = request.Title ?? task.Title;
        task.Notes = request.Notes ?? task.Notes;
        task.Data = string.IsNullOrWhiteSpace(request.Data) ? task.Data : request.Data;
        task.DueOn = request.DueOn ?? task.DueOn;
        task.DueTime = request.DueTime ?? task.DueTime;

        await _tasksRepository.UpdateAsync(task, unitOfWork);

        return new UpdateTaskResponse
        {
            Task = await _tasksRepository.GetByIdAsync(task.Id, unitOfWork)
                   ?? throw new NotFoundException("Task not found after update", ExceptionCodes.TASK_NOT_FOUND)
        };
    }
}
