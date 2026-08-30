using Dapper;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;

namespace RandomTaskTrack.Business.Repositories.Learning;

public class LearningRepository : ILearningRepository
{
    private const string GoalColumns = @"
        id, title, tier, status, why, benefits, target_on, context,
        plan::text AS plan, researched_at, research_model, notes, created_at, updated_at";

    private const string StepColumns = @"
        id, goal_id, title, kind, status, target_on, notes, outcome,
        provider, url, cost, hours, sort_order, created_at, updated_at";

    private const string CredentialColumns = @"
        id, goal_id, name, issuer, code, earned_on, renewal_kind, expires_on,
        credential_id, url, renewal::text AS renewal, researched_at, research_model,
        notes, created_at, updated_at";

    private const string TaskColumns = @"
        t.id, t.domain_id, d.code AS domain_code, d.name AS domain_name,
        t.recurrence_id, t.title, t.notes, t.data::text AS data,
        t.due_on, t.due_time, t.status, t.completed_at";

    /// <summary>
    /// The links are jsonb fields rather than columns (see the learning
    /// migration), so these filters cannot use an index. They do not need to:
    /// both are already narrowed to pending rows, of which there are never many.
    /// </summary>
    private const string ByStepId = "data ->> 'learnStepId' = ANY(@ids)";

    private const string ByCredentialId = "data ->> 'credentialId' = ANY(@ids)";

    // ── Goals ────────────────────────────────────────────────────────────────

    public async Task<List<LearningGoal>> GetGoalsAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<LearningGoal>(
            $@"SELECT {GoalColumns}
               FROM tracker.learn_goals
               ORDER BY tier, created_at",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<LearningGoal?> GetGoalAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<LearningGoal>(
            $"SELECT {GoalColumns} FROM tracker.learn_goals WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateGoalAsync(LearningGoal goal, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.learn_goals
                  (id, title, tier, status, why, benefits, target_on, context, notes)
              VALUES
                  (@Id, @Title, @Tier, @Status, @Why, @Benefits, @TargetOn, @Context, @Notes)",
            goal,
            unitOfWork.Transaction);
    }

    public async Task UpdateGoalAsync(LearningGoal goal, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.learn_goals
              SET title      = @Title,
                  tier       = @Tier,
                  status     = @Status,
                  why        = @Why,
                  benefits   = @Benefits,
                  target_on  = @TargetOn,
                  context    = @Context,
                  notes      = @Notes,
                  updated_at = now()
              WHERE id = @Id",
            goal,
            unitOfWork.Transaction);
    }

    public async Task SavePlanAsync(LearningGoal goal, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.learn_goals
              SET context        = @Context,
                  plan           = @Plan::jsonb,
                  researched_at  = @ResearchedAt,
                  research_model = @ResearchModel,
                  updated_at     = now()
              WHERE id = @Id",
            goal,
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteGoalAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.learn_goals WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    // ── Steps ────────────────────────────────────────────────────────────────

    public async Task<List<LearningStep>> GetStepsAsync(IEnumerable<Guid> goalIds, IUnitOfWork unitOfWork)
    {
        Guid[] ids = goalIds.ToArray();

        if (ids.Length == 0)
        {
            return new List<LearningStep>();
        }

        var rows = await unitOfWork.Connection.QueryAsync<LearningStep>(
            $@"SELECT {StepColumns}
               FROM tracker.learn_steps
               WHERE goal_id = ANY(@ids)
               ORDER BY sort_order, created_at",
            new { ids },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<LearningStep?> GetStepAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<LearningStep>(
            $"SELECT {StepColumns} FROM tracker.learn_steps WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateStepAsync(LearningStep step, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.learn_steps
                  (id, goal_id, title, kind, status, target_on, notes, outcome,
                   provider, url, cost, hours, sort_order)
              VALUES
                  (@Id, @GoalId, @Title, @Kind, @Status, @TargetOn, @Notes, @Outcome,
                   @Provider, @Url, @Cost, @Hours, @SortOrder)",
            step,
            unitOfWork.Transaction);
    }

    public async Task UpdateStepAsync(LearningStep step, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.learn_steps
              SET title      = @Title,
                  kind       = @Kind,
                  status     = @Status,
                  target_on  = @TargetOn,
                  notes      = @Notes,
                  outcome    = @Outcome,
                  provider   = @Provider,
                  url        = @Url,
                  cost       = @Cost,
                  hours      = @Hours,
                  sort_order = @SortOrder,
                  updated_at = now()
              WHERE id = @Id",
            step,
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteStepAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.learn_steps WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    public async Task<int> GetMaxStepSortOrderAsync(Guid goalId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(
            @"SELECT COALESCE(MAX(sort_order), 0)
              FROM tracker.learn_steps
              WHERE goal_id = @goalId",
            new { goalId },
            unitOfWork.Transaction);
    }

    // ── Credentials ──────────────────────────────────────────────────────────

    public async Task<List<LearningCredential>> GetCredentialsAsync(IUnitOfWork unitOfWork)
    {
        // NULLS LAST puts the permanent ones and the unchecked ones under the
        // ones with a clock, which is the order the Achieved pane reads in.
        var rows = await unitOfWork.Connection.QueryAsync<LearningCredential>(
            $@"SELECT {CredentialColumns}
               FROM tracker.learn_credentials
               ORDER BY expires_on NULLS LAST, earned_on DESC",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<LearningCredential?> GetCredentialAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<LearningCredential>(
            $"SELECT {CredentialColumns} FROM tracker.learn_credentials WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateCredentialAsync(LearningCredential credential, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.learn_credentials
                  (id, goal_id, name, issuer, code, earned_on, renewal_kind, expires_on,
                   credential_id, url, notes)
              VALUES
                  (@Id, @GoalId, @Name, @Issuer, @Code, @EarnedOn, @RenewalKind, @ExpiresOn,
                   @CredentialId, @Url, @Notes)",
            credential,
            unitOfWork.Transaction);
    }

    public async Task UpdateCredentialAsync(LearningCredential credential, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.learn_credentials
              SET goal_id       = @GoalId,
                  name          = @Name,
                  issuer        = @Issuer,
                  code          = @Code,
                  earned_on     = @EarnedOn,
                  renewal_kind  = @RenewalKind,
                  expires_on    = @ExpiresOn,
                  credential_id = @CredentialId,
                  url           = @Url,
                  notes         = @Notes,
                  updated_at    = now()
              WHERE id = @Id",
            credential,
            unitOfWork.Transaction);
    }

    public async Task SaveRenewalAsync(LearningCredential credential, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.learn_credentials
              SET renewal_kind   = @RenewalKind,
                  expires_on     = @ExpiresOn,
                  renewal        = @Renewal::jsonb,
                  researched_at  = @ResearchedAt,
                  research_model = @ResearchModel,
                  updated_at     = now()
              WHERE id = @Id",
            credential,
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteCredentialAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.learn_credentials WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }

    // ── The task-engine side ─────────────────────────────────────────────────

    public Task<List<TaskListItemDto>> GetStepTasksAsync(IEnumerable<Guid> stepIds, IUnitOfWork unitOfWork) =>
        GetTasksAsync(stepIds, ByStepId, unitOfWork);

    public Task<List<TaskListItemDto>> GetCredentialTasksAsync(IEnumerable<Guid> credentialIds, IUnitOfWork unitOfWork) =>
        GetTasksAsync(credentialIds, ByCredentialId, unitOfWork);

    private async Task<List<TaskListItemDto>> GetTasksAsync(IEnumerable<Guid> keys, string predicate, IUnitOfWork unitOfWork)
    {
        string[] ids = ToTextIds(keys);

        if (ids.Length == 0)
        {
            return new List<TaskListItemDto>();
        }

        var rows = await unitOfWork.Connection.QueryAsync<TaskListItemDto>(
            $@"SELECT {TaskColumns}
               FROM tracker.task_tasks t
               INNER JOIN tracker.task_domains d ON d.id = t.domain_id
               WHERE t.status = @pending
                 AND t.{predicate}
               ORDER BY t.due_on, t.title",
            new { ids, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<int> DeleteStepTasksAsync(IEnumerable<Guid> stepIds, IUnitOfWork unitOfWork)
    {
        string[] ids = ToTextIds(stepIds);

        if (ids.Length == 0)
        {
            return 0;
        }

        return await unitOfWork.Connection.ExecuteAsync(
            $@"DELETE FROM tracker.task_tasks
               WHERE status = @pending
                 AND {ByStepId}",
            new { ids, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);
    }

    public async Task<int> DeleteCredentialTasksAsync(Guid credentialId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteAsync(
            $@"DELETE FROM tracker.task_tasks
               WHERE status = @pending
                 AND {ByCredentialId}",
            new { ids = new[] { credentialId.ToString() }, pending = (int)TaskItemStatus.Pending },
            unitOfWork.Transaction);
    }

    /// <summary>
    /// jsonb ->> gives text, so the ids go over as text too. Comparing them as
    /// uuid would fail the whole query the first time any other scope wrote a
    /// non-uuid value under one of these keys.
    /// </summary>
    private static string[] ToTextIds(IEnumerable<Guid> ids) =>
        ids.Select(id => id.ToString()).ToArray();
}
