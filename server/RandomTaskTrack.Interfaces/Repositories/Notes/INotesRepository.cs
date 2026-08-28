using RandomTaskTrack.Data.Models.Notes;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Notes;

public interface INotesRepository
{
    Task<List<Note>> GetAllAsync(IUnitOfWork unitOfWork);
    Task<Note?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork);
    Task CreateAsync(Note note, IUnitOfWork unitOfWork);
    Task UpdateAsync(Note note, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork);
}