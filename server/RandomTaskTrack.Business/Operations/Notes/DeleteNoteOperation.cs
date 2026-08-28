using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Notes;
using RandomTaskTrack.Data.Response.Notes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Notes;

namespace RandomTaskTrack.Business.Operations.Notes;

public class DeleteNoteOperation : BaseOperation<DeleteNoteRequest, DeleteNoteResponse>
{
    private readonly INotesRepository _notesRepository;

    public DeleteNoteOperation(
        ILogger<DeleteNoteOperation> logger,
        IValidator<DeleteNoteRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        INotesRepository notesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _notesRepository = notesRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteNoteResponse> Execute(DeleteNoteRequest request, IUnitOfWork unitOfWork)
    {
        bool deleted = await _notesRepository.DeleteAsync(request.Id, unitOfWork);

        if (!deleted)
        {
            throw new NotFoundException($"No note with id {request.Id}", ExceptionCodes.NOTE_NOT_FOUND);
        }

        return new DeleteNoteResponse { Success = true };
    }
}