using System.Text;
using Dapper;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Repositories.Tasks;

public class CompletionsRepository : ICompletionsRepository
{
    public async Task CreateAsync(TaskCompletion completion, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.task_completions
                  (id, task_id, domain_id, status, planned_data, actual_data, note, due_on, completed_at)
              VALUES
                  (@Id, @TaskId, @DomainId, @Status, @PlannedData::jsonb, @ActualData::jsonb, @Note, @DueOn, @CompletedAt)",
            new
            {
                completion.Id,
                completion.TaskId,
                completion.DomainId,
                Status = (int)completion.Status,
                completion.PlannedData,
                completion.ActualData,
                completion.Note,
                completion.DueOn,
                completion.CompletedAt
            },
            unitOfWork.Transaction);
    }

    public async Task<List<CompletionLogItemDto>> QueryAsync(
        int? domainId,
        string? titleContains,
        DateOnly? fromDate,
        DateOnly? toDate,
        int limit,
        IUnitOfWork unitOfWork)
    {
        // Title lives on the task, not the completion, so the join is required
        // even for a pure log read.
        var sql = new StringBuilder(@"
            SELECT c.id,
                   c.task_id,
                   c.domain_id,
                   d.code AS domain_code,
                   t.title,
                   c.status,
                   c.planned_data::text AS planned_data,
                   c.actual_data::text  AS actual_data,
                   c.note,
                   c.due_on,
                   c.completed_at
            FROM tracker.task_completions c
            INNER JOIN tracker.task_domains d ON d.id = c.domain_id
            INNER JOIN tracker.task_tasks   t ON t.id = c.task_id");

        var clauses = new List<string>();
        if (domainId.HasValue) clauses.Add("c.domain_id = @domainId");
        if (fromDate.HasValue) clauses.Add("c.due_on >= @fromDate");
        if (toDate.HasValue) clauses.Add("c.due_on <= @toDate");
        if (!string.IsNullOrWhiteSpace(titleContains)) clauses.Add("t.title ILIKE @titleContains");

        if (clauses.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", clauses));
        }

        sql.Append(" ORDER BY c.completed_at DESC LIMIT @limit");

        var rows = await unitOfWork.Connection.QueryAsync<CompletionLogItemDto>(
            sql.ToString(),
            new
            {
                domainId,
                fromDate,
                toDate,
                titleContains = string.IsNullOrWhiteSpace(titleContains) ? null : $"%{titleContains.Trim()}%",
                limit
            },
            unitOfWork.Transaction);

        return rows.ToList();
    }
}
