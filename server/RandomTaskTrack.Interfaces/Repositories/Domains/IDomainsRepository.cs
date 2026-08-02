using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Domains;

public interface IDomainsRepository
{
    Task<List<TaskDomain>> GetAllAsync(bool includeInactive, IUnitOfWork unitOfWork);
    Task<TaskDomain?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<TaskDomain?> GetByCodeAsync(string code, IUnitOfWork unitOfWork);
}
