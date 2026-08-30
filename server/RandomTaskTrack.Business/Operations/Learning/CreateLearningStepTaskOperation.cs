using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Puts a step on the board, so it turns up on Today next to the watering and
/// lands in the completion log like everything else. That is the whole point of
/// the learning domain existing: studying that never reaches the dashboard is
/// studying that competes with nothing and therefore loses to everything.
///
/// A one-off dated task, never a recurrence — a step is done once. Study that
/// repeats ("an hour of Spanish every weekday") is a recurrence, and the
/// Recurring tab is the right place to make one.
/// </summary>
public class CreateLearningStepTaskOperation : BaseOperation<CreateLearningStepTaskRequest, CreateLearningStepTaskResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IClock _clock;

    public CreateLearningStepTaskOperation(
        ILogger<CreateLearningStepTaskOperation> logger,
        IValidator<CreateLearningStepTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateLearningStepTaskResponse> Execute(CreateLearningStepTaskRequest request, IUnitOfWork unitOfWork)
    {
        LearningStep step = await _learningRepository.GetStepAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning step with id {request.Id}", ExceptionCodes.LEARNING_STEP_NOT_FOUND);

        LearningGoal goal = await _learningRepository.GetGoalAsync(step.GoalId, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {step.GoalId}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        TaskDomain domain = await _domainsRepository.GetByCodeAsync(DomainCodes.Learning, unitOfWork)
                            ?? throw new NotFoundException($"No '{DomainCodes.Learning}' domain to file the step under.", ExceptionCodes.DOMAIN_NOT_FOUND);

        List<TaskListItemDto> existing = await _learningRepository.GetStepTasksAsync([step.Id], unitOfWork);

        // Already on the board. A second press is a double-tap, not a request
        // for a duplicate; the goal comes back either way so nothing is hidden.
        if (existing.Count > 0)
        {
            return new CreateLearningStepTaskResponse
            {
                Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork)
            };
        }

        await _tasksRepository.CreateAsync(new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = domain.Id,

            // The goal is in the title because the dashboard mixes every
            // tracker together, and "Assignment 3" says nothing next to a
            // watering and a bin day.
            Title = $"{step.Title} — {goal.Title}",
            Notes = string.IsNullOrWhiteSpace(step.Notes) ? null : step.Notes,
            Data = LearningMapper.PayloadForStep(step.Id, goal.Id),

            // The step's own date if it has one. Falling back to today rather
            // than refusing: a step put on the board without a date is one the
            // user means to start now.
            DueOn = request.DueOn ?? step.TargetOn ?? _clock.Today,
            Status = TaskItemStatus.Pending
        }, unitOfWork);

        // Putting it on the board is the moment it stopped being a plan.
        if (step.Status == LearningStepStatus.Planned)
        {
            step.Status = LearningStepStatus.Doing;
            await _learningRepository.UpdateStepAsync(step, unitOfWork);
        }

        return new CreateLearningStepTaskResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }
}
