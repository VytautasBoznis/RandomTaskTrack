using Dapper;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;

namespace RandomTaskTrack.Business.Repositories.Domains;

public class DomainsRepository : IDomainsRepository
{
    private const string SelectColumns = "id, code, name, is_active, sort_order";

    public async Task<List<TaskDomain>> GetAllAsync(bool includeInactive, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<TaskDomain>(
            $@"SELECT {SelectColumns}
               FROM tracker.task_domains
               WHERE (@includeInactive OR is_active)
               ORDER BY sort_order, name",
            new { includeInactive },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<TaskDomain?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<TaskDomain>(
            $"SELECT {SelectColumns} FROM tracker.task_domains WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<TaskDomain?> GetByCodeAsync(string code, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<TaskDomain>(
            $"SELECT {SelectColumns} FROM tracker.task_domains WHERE lower(code) = lower(@code)",
            new { code },
            unitOfWork.Transaction);
    }
}
