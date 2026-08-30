using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Learning;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Finds out how a held credential renews — including that it does not.
///
/// **The lookup fills in what nobody has decided; it never overwrites a
/// decision.** If the renewal kind has been set, or an expiry date typed, that
/// answer stands and the lookup contributes only its prose — how renewal works,
/// what it costs, what happens if it lapses. This is the rule a plant's
/// hand-typed species follows, and it matters more here: the person holding the
/// certificate knows what it says, and a search result should not talk them out
/// of it.
/// </summary>
public class ResearchCredentialOperation : BaseOperation<ResearchCredentialRequest, ResearchCredentialResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly ILearningResearcher _researcher;
    private readonly IClock _clock;

    public ResearchCredentialOperation(
        ILogger<ResearchCredentialOperation> logger,
        IValidator<ResearchCredentialRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        ILearningResearcher researcher,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _researcher = researcher;
        _clock = clock;
    }

    /// <summary>One UPDATE behind a network call. See DraftLearningPlanOperation.</summary>
    protected override bool RequiresTransaction => false;

    protected override async Task<ResearchCredentialResponse> Execute(ResearchCredentialRequest request, IUnitOfWork unitOfWork)
    {
        LearningCredential credential = await _learningRepository.GetCredentialAsync(request.Id, unitOfWork)
                                        ?? throw new NotFoundException($"No credential with id {request.Id}", ExceptionCodes.LEARNING_CREDENTIAL_NOT_FOUND);

        CredentialResearchResult result = await _researcher.ResearchCredentialAsync(
            new CredentialQuestion
            {
                Name = credential.Name,
                Issuer = credential.Issuer,
                Code = credential.Code,
                EarnedOn = credential.EarnedOn
            },
            CancellationToken.None);

        Apply(credential, result);

        credential.Renewal = LearningMapper.Serialize(result.Renewal);
        credential.ResearchedAt = DateTime.UtcNow;
        credential.ResearchModel = result.Model;

        await _learningRepository.SaveRenewalAsync(credential, unitOfWork);

        return new ResearchCredentialResponse
        {
            Credential = await LearningLoader.LoadCredentialAsync(credential.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    private void Apply(LearningCredential credential, CredentialResearchResult result)
    {
        // A typed expiry is a decision even when the kind was left alone: you do
        // not put a date on something unless you believe it runs out. Treating
        // it as one is what stops the lookup quietly deleting it to satisfy
        // ck_learn_credentials_renewal.
        bool alreadyDecided = credential.RenewalKind != CredentialRenewalKind.Unknown ||
                              credential.ExpiresOn.HasValue;

        if (alreadyDecided)
        {
            _logger.LogInformation(
                "Credential {Id} already states how it renews — keeping it and storing the lookup's notes only",
                credential.Id);

            return;
        }

        credential.RenewalKind = result.RenewalKind;

        credential.ExpiresOn = result.RenewalKind == CredentialRenewalKind.Expires && result.Renewal.ValidityMonths is { } months
            ? credential.EarnedOn.AddMonths(months)

            // Permanent and Unknown both carry no date, which is what the
            // database CHECK requires of the first and what honesty requires of
            // the second.
            : null;
    }
}
