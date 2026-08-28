using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Notes;
using RandomTaskTrack.Data.Response.Notes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Notes;

namespace RandomTaskTrack.Business.Operations.Notes;

public class GetNotesOperation : BaseOperation<GetNotesRequest, GetNotesResponse>
{
    private readonly INotesRepository _notesRepository;

    public GetNotesOperation(
        ILogger<GetNotesOperation> logger,
        IValidator<GetNotesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        INotesRepository notesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _notesRepository = notesRepository;
    }

    protected override async Task<GetNotesResponse> Execute(GetNotesRequest request, IUnitOfWork unitOfWork)
    {
        return new GetNotesResponse
        {
            Notes = await _notesRepository.GetAllAsync(unitOfWork)
        };
    }
}