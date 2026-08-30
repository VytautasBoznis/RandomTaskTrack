using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Learning;

public interface ILearningRepository
{
    // ── Goals ────────────────────────────────────────────────────────────────

    /// <summary>Every path, by tier then by age. The tab's only ordering.</summary>
    Task<List<LearningGoal>> GetGoalsAsync(IUnitOfWork unitOfWork);

    Task<LearningGoal?> GetGoalAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateGoalAsync(LearningGoal goal, IUnitOfWork unitOfWork);
    Task UpdateGoalAsync(LearningGoal goal, IUnitOfWork unitOfWork);

    /// <summary>
    /// Writes back a finished draft only. Split from UpdateGoalAsync for the
    /// reason SaveProfileAsync is split from UpdateAsync on plants: the user
    /// owns the title, the why and the target, the draft owns the plan, and
    /// neither should overwrite the other.
    /// </summary>
    Task SavePlanAsync(LearningGoal goal, IUnitOfWork unitOfWork);

    /// <summary>Takes the steps with it — learn_steps cascades. The tasks it does
    /// not, which is what DeleteLearningGoalOperation is for.</summary>
    Task<bool> DeleteGoalAsync(Guid id, IUnitOfWork unitOfWork);

    // ── Steps ────────────────────────────────────────────────────────────────

    Task<List<LearningStep>> GetStepsAsync(IEnumerable<Guid> goalIds, IUnitOfWork unitOfWork);
    Task<LearningStep?> GetStepAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateStepAsync(LearningStep step, IUnitOfWork unitOfWork);
    Task UpdateStepAsync(LearningStep step, IUnitOfWork unitOfWork);
    Task<bool> DeleteStepAsync(Guid id, IUnitOfWork unitOfWork);

    /// <summary>The highest sort_order on a path, so appended steps land at the
    /// bottom instead of all sharing 0.</summary>
    Task<int> GetMaxStepSortOrderAsync(Guid goalId, IUnitOfWork unitOfWork);

    // ── Credentials ──────────────────────────────────────────────────────────

    /// <summary>Everything held: expiring soonest first, then the ones with no
    /// clock on them.</summary>
    Task<List<LearningCredential>> GetCredentialsAsync(IUnitOfWork unitOfWork);

    Task<LearningCredential?> GetCredentialAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateCredentialAsync(LearningCredential credential, IUnitOfWork unitOfWork);
    Task UpdateCredentialAsync(LearningCredential credential, IUnitOfWork unitOfWork);

    /// <summary>Writes back a finished renewal lookup only. Same split as
    /// <see cref="SavePlanAsync"/>.</summary>
    Task SaveRenewalAsync(LearningCredential credential, IUnitOfWork unitOfWork);

    Task<bool> DeleteCredentialAsync(Guid id, IUnitOfWork unitOfWork);

    // ── The task-engine side, joined on the payload ──────────────────────────

    /// <summary>Pending tasks carrying any of these step ids, soonest first.</summary>
    Task<List<TaskListItemDto>> GetStepTasksAsync(IEnumerable<Guid> stepIds, IUnitOfWork unitOfWork);

    /// <summary>Pending renewal reminders for these credentials.</summary>
    Task<List<TaskListItemDto>> GetCredentialTasksAsync(IEnumerable<Guid> credentialIds, IUnitOfWork unitOfWork);

    /// <summary>
    /// Every pending task for these steps, overdue ones included. Called when
    /// the step or its whole path is being deleted, so "sit AZ-305, 3 weeks
    /// late" is noise rather than history. Completed rows are never touched.
    /// </summary>
    Task<int> DeleteStepTasksAsync(IEnumerable<Guid> stepIds, IUnitOfWork unitOfWork);

    /// <summary>The same, for a credential's reminders.</summary>
    Task<int> DeleteCredentialTasksAsync(Guid credentialId, IUnitOfWork unitOfWork);
}
