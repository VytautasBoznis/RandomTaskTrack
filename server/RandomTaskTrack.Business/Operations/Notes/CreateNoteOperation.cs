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

public class CreateNoteOperation : BaseOperation<CreateNoteRequest, CreateNoteResponse>
{
    private readonly INotesRepository _notesRepository;

    public CreateNoteOperation(
        ILogger<CreateNoteOperation> logger,
        IValidator<CreateNoteRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        INotesRepository notesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _notesRepository = notesRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateNoteResponse> Execute(CreateNoteRequest request, IUnitOfWork unitOfWork)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content ?? ""
        };

        await _notesRepository.CreateAsync(note, unitOfWork);

        return new CreateNoteResponse
        {
            Note = await _notesRepository.GetByIdAsync(note.Id, unitOfWork)
                   ?? throw new NotFoundException("Note not found after create", ExceptionCodes.NOTE_NOT_FOUND)
        };
    }
}