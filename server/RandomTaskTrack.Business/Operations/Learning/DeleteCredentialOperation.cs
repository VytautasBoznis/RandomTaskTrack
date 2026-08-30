using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Takes the credential and any renewal reminder it had on the board — the
/// payload link means nothing in the database would.
/// </summary>
public class DeleteCredentialOperation : BaseOperation<DeleteCredentialRequest, DeleteCredentialResponse>
{
    private readonly ILearningRepository _learningRepository;

    public DeleteCredentialOperation(
        ILogger<DeleteCredentialOperation> logger,
        IValidator<DeleteCredentialRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteCredentialResponse> Execute(DeleteCredentialRequest request, IUnitOfWork unitOfWork)
    {
        LearningCredential credential = await _learningRepository.GetCredentialAsync(request.Id, unitOfWork)
                                        ?? throw new NotFoundException($"No credential with id {request.Id}", ExceptionCodes.LEARNING_CREDENTIAL_NOT_FOUND);

        int deletedTasks = await _learningRepository.DeleteCredentialTasksAsync(credential.Id, unitOfWork);

        return new DeleteCredentialResponse
        {
            Success = await _learningRepository.DeleteCredentialAsync(credential.Id, unitOfWork),
            DeletedTaskCount = deletedTasks
        };
    }
}
