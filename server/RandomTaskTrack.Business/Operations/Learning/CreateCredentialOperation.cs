using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Records something already held. Does not look the renewal up: that is its
/// own button, because it is a web search that takes a moment and because the
/// answer is often already known — nobody needs a search to be told an old
/// MCSD is permanent.
/// </summary>
public class CreateCredentialOperation : BaseOperation<CreateCredentialRequest, CreateCredentialResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public CreateCredentialOperation(
        ILogger<CreateCredentialOperation> logger,
        IValidator<CreateCredentialRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<CreateCredentialResponse> Execute(CreateCredentialRequest request, IUnitOfWork unitOfWork)
    {
        var credential = new LearningCredential
        {
            Id = Guid.NewGuid(),
            GoalId = request.GoalId,
            Name = request.Name.Trim(),
            Issuer = request.Issuer?.Trim() ?? "",
            Code = Clean(request.Code),
            EarnedOn = request.EarnedOn,
            RenewalKind = request.RenewalKind,
            ExpiresOn = request.ExpiresOn,
            CredentialId = Clean(request.CredentialId),
            Url = Clean(request.Url),
            Notes = request.Notes?.Trim() ?? ""
        };

        await _learningRepository.CreateCredentialAsync(credential, unitOfWork);

        return new CreateCredentialResponse
        {
            Credential = await LearningLoader.LoadCredentialAsync(credential.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
