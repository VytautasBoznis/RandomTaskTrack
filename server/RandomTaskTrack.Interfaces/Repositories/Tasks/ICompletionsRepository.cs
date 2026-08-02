using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Tasks;

public interface ICompletionsRepository
{
    Task CreateAsync(TaskCompletion completion, IUnitOfWork unitOfWork);

    Task<List<CompletionLogItemDto>> QueryAsync(
        int? domainId,
        string? titleContains,
        DateOnly? fromDate,
        DateOnly? toDate,
        int limit,
        IUnitOfWork unitOfWork);
}
