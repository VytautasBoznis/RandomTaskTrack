using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;

namespace RandomTaskTrack.Business.Learning;

/// <summary>
/// Reads one goal or one credential back with everything its card renders.
/// Every write operation ends this way, so what the UI re-renders is the thing
/// as it now stands rather than the request echoed back.
/// </summary>
internal static class LearningLoader
{
    public static async Task<LearningGoalDto> LoadGoalAsync(
        Guid goalId,
        DateOnly today,
        ILearningRepository repository,
        IUnitOfWork unitOfWork)
    {
        LearningGoal goal = await repository.GetGoalAsync(goalId, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {goalId}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        List<LearningStep> steps = await repository.GetStepsAsync([goal.Id], unitOfWork);

        return LearningMapper.ToDto(goal, await LoadStepsAsync(steps, repository, unitOfWork), today);
    }

    public static async Task<LearningCredentialDto> LoadCredentialAsync(
        Guid credentialId,
        DateOnly today,
        ILearningRepository repository,
        IUnitOfWork unitOfWork)
    {
        LearningCredential credential = await repository.GetCredentialAsync(credentialId, unitOfWork)
                                        ?? throw new NotFoundException($"No credential with id {credentialId}", ExceptionCodes.LEARNING_CREDENTIAL_NOT_FOUND);

        List<TaskListItemDto> tasks = await repository.GetCredentialTasksAsync([credential.Id], unitOfWork);

        return LearningMapper.ToDto(credential, tasks.FirstOrDefault(), today);
    }

    /// <summary>
    /// Attaches each step's pending task, in one query for all of them rather
    /// than one per step. A step can only have one reminder on the board at a
    /// time — the create guards against a second — so the first is the one.
    /// </summary>
    public static async Task<List<LearningStepDto>> LoadStepsAsync(
        List<LearningStep> steps,
        ILearningRepository repository,
        IUnitOfWork unitOfWork)
    {
        if (steps.Count == 0)
        {
            return new List<LearningStepDto>();
        }

        List<TaskListItemDto> tasks =
            await repository.GetStepTasksAsync(steps.Select(step => step.Id), unitOfWork);

        Dictionary<Guid, TaskListItemDto> byStep = tasks
            .Select(task => (StepId: LearningMapper.StepIdOf(task.Data), Task: task))
            .Where(pair => pair.StepId.HasValue)
            .GroupBy(pair => pair.StepId!.Value)
            .ToDictionary(group => group.Key, group => group.First().Task);

        return steps
            .Select(step => LearningMapper.ToDto(step, byStep.GetValueOrDefault(step.Id)))
            .ToList();
    }
}
