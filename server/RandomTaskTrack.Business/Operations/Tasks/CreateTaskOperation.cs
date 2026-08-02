using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class CreateTaskOperation : BaseOperation<CreateTaskRequest, CreateTaskResponse>
{
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;

    public CreateTaskOperation(
        ILogger<CreateTaskOperation> logger,
        IValidator<CreateTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateTaskResponse> Execute(CreateTaskRequest request, IUnitOfWork unitOfWork)
    {
        _ = await _domainsRepository.GetByIdAsync(request.DomainId, unitOfWork)
            ?? throw new NotFoundException($"No domain with id {request.DomainId}", ExceptionCodes.DOMAIN_NOT_FOUND);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = request.DomainId,
            Title = request.Title,
            Notes = request.Notes,
            Data = string.IsNullOrWhiteSpace(request.Data) ? "{}" : request.Data,
            DueOn = request.DueOn,
            DueTime = request.DueTime,
            Status = TaskItemStatus.Pending
        };

        await _tasksRepository.CreateAsync(task, unitOfWork);

        return new CreateTaskResponse
        {
            Task = await _tasksRepository.GetByIdAsync(task.Id, unitOfWork)
                   ?? throw new NotFoundException("Task not found after create", ExceptionCodes.TASK_NOT_FOUND)
        };
    }
}
