using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Takes the path and everything it put on the board.
///
/// learn_steps cascades, so the steps go with the goal for free. The tasks do
/// not: they are joined by payload rather than by foreign key, which is the
/// price of keeping task_tasks ignorant of this scope. Completed history stays
/// — what was studied was studied.
///
/// Credentials survive on purpose. learn_credentials.goal_id is ON DELETE SET
/// NULL because deleting a path must never delete the evidence that something
/// was passed on the way through it.
/// </summary>
public class DeleteLearningGoalOperation : BaseOperation<DeleteLearningGoalRequest, DeleteLearningGoalResponse>
{
    private readonly ILearningRepository _learningRepository;

    public DeleteLearningGoalOperation(
        ILogger<DeleteLearningGoalOperation> logger,
        IValidator<DeleteLearningGoalRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteLearningGoalResponse> Execute(DeleteLearningGoalRequest request, IUnitOfWork unitOfWork)
    {
        LearningGoal goal = await _learningRepository.GetGoalAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {request.Id}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        List<LearningStep> steps = await _learningRepository.GetStepsAsync([goal.Id], unitOfWork);

        // Before the cascade takes the steps: once they are gone there is
        // nothing left to find their tasks by.
        int deletedTasks = await _learningRepository.DeleteStepTasksAsync(
            steps.Select(step => step.Id),
            unitOfWork);

        return new DeleteLearningGoalResponse
        {
            Success = await _learningRepository.DeleteGoalAsync(goal.Id, unitOfWork),
            DeletedTaskCount = deletedTasks
        };
    }
}
