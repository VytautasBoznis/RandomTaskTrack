using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Notes;
using RandomTaskTrack.Data.Request.Notes;
using RandomTaskTrack.Data.Response.Notes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Notes;

namespace RandomTaskTrack.Business.Operations.Notes;

public class UpdateNoteOperation : BaseOperation<UpdateNoteRequest, UpdateNoteResponse>
{
    private readonly INotesRepository _notesRepository;

    public UpdateNoteOperation(
        ILogger<UpdateNoteOperation> logger,
        IValidator<UpdateNoteRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        INotesRepository notesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _notesRepository = notesRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateNoteResponse> Execute(UpdateNoteRequest request, IUnitOfWork unitOfWork)
    {
        Note note = await _notesRepository.GetByIdAsync(request.Id, unitOfWork)
                    ?? throw new NotFoundException($"No note with id {request.Id}", ExceptionCodes.NOTE_NOT_FOUND);

        // Null means "leave alone", the same as everywhere else. An empty
        // Content is a real value here, though — emptying a note is how you
        // clear it without deleting it.
        note.Title = request.Title ?? note.Title;
        note.Content = request.Content ?? note.Content;

        await _notesRepository.UpdateAsync(note, unitOfWork);

        return new UpdateNoteResponse
        {
            Note = await _notesRepository.GetByIdAsync(note.Id, unitOfWork)
                   ?? throw new NotFoundException("Note not found after update", ExceptionCodes.NOTE_NOT_FOUND)
        };
    }
}