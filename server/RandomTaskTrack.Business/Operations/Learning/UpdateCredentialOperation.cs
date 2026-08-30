using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Also how a renewal is recorded: move the earned date forward and push the
/// expiry out. Same row, because a renewed credential is the same credential —
/// a second row would leave the old expiry behind, still counting down, still
/// holding its reminder.
///
/// A pending reminder for the old date is cleared when the expiry moves, for
/// the same reason. It was put on the board to prompt exactly the thing that
/// has now happened.
/// </summary>
public class UpdateCredentialOperation : BaseOperation<UpdateCredentialRequest, UpdateCredentialResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public UpdateCredentialOperation(
        ILogger<UpdateCredentialOperation> logger,
        IValidator<UpdateCredentialRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateCredentialResponse> Execute(UpdateCredentialRequest request, IUnitOfWork unitOfWork)
    {
        LearningCredential credential = await _learningRepository.GetCredentialAsync(request.Id, unitOfWork)
                                        ?? throw new NotFoundException($"No credential with id {request.Id}", ExceptionCodes.LEARNING_CREDENTIAL_NOT_FOUND);

        bool expiryMoved = credential.ExpiresOn != request.ExpiresOn ||
                           credential.RenewalKind != request.RenewalKind;

        credential.GoalId = request.GoalId;
        credential.Name = request.Name.Trim();
        credential.Issuer = request.Issuer?.Trim() ?? "";
        credential.Code = Clean(request.Code);
        credential.EarnedOn = request.EarnedOn;
        credential.RenewalKind = request.RenewalKind;
        credential.ExpiresOn = request.ExpiresOn;
        credential.CredentialId = Clean(request.CredentialId);
        credential.Url = Clean(request.Url);
        credential.Notes = request.Notes?.Trim() ?? "";

        await _learningRepository.UpdateCredentialAsync(credential, unitOfWork);

        if (expiryMoved)
        {
            await _learningRepository.DeleteCredentialTasksAsync(credential.Id, unitOfWork);
        }

        return new UpdateCredentialResponse
        {
            Credential = await LearningLoader.LoadCredentialAsync(credential.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
