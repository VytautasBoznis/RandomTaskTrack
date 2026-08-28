using Dapper;
using RandomTaskTrack.Data.Models.Notes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Notes;

namespace RandomTaskTrack.Business.Repositories.Notes;

public class NotesRepository : INotesRepository
{
    private const string SelectColumns = "id, title, content, created_at, updated_at";

    public async Task<List<Note>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<Note>(
            $@"SELECT {SelectColumns}
               FROM tracker.note_notes
               ORDER BY updated_at DESC",
            transaction: unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<Note?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Note>(
            $"SELECT {SelectColumns} FROM tracker.note_notes WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task CreateAsync(Note note, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.note_notes (id, title, content)
              VALUES (@Id, @Title, @Content)",
            new { note.Id, note.Title, note.Content },
            unitOfWork.Transaction);
    }

    public async Task UpdateAsync(Note note, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"UPDATE tracker.note_notes
              SET title      = @Title,
                  content    = @Content,
                  updated_at = now()
              WHERE id = @Id",
            new { note.Id, note.Title, note.Content },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.note_notes WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }
}