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
/// Drafts the route to a goal, or drafts it again with a better brief.
///
/// A re-draft replaces the suggestion and nothing else. Steps already committed
/// to survive it untouched — that is the whole reason they are a table rather
/// than part of the blob, and it is what makes asking again cheap enough to be
/// worth doing when the target date slips or the starting point changes.
///
/// Failure is reported as failure, unlike the create path on plants: this
/// button does exactly one thing, so silently doing nothing would be a lie.
/// </summary>
public class DraftLearningPlanOperation : BaseOperation<DraftLearningPlanRequest, DraftLearningPlanResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly ILearningResearcher _researcher;
    private readonly IClock _clock;

    public DraftLearningPlanOperation(
        ILogger<DraftLearningPlanOperation> logger,
        IValidator<DraftLearningPlanRequest> validator,
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

    /// <summary>One UPDATE behind a network call that can take a minute. Holding
    /// a transaction open across it would buy nothing.</summary>
    protected override bool RequiresTransaction => false;

    protected override async Task<DraftLearningPlanResponse> Execute(DraftLearningPlanRequest request, IUnitOfWork unitOfWork)
    {
        LearningGoal goal = await _learningRepository.GetGoalAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {request.Id}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        // A new brief replaces the stored one: it is what the next re-draft
        // should ask, and keeping the old one would make the plan untraceable
        // to the question that produced it.
        if (request.Context is not null)
        {
            goal.Context = request.Context.Trim();
        }

        List<LearningCredential> credentials = await _learningRepository.GetCredentialsAsync(unitOfWork);

        LearningPlanResult result = await _researcher.DraftPlanAsync(
            new LearningPlanQuestion
            {
                Title = goal.Title,
                Why = goal.Why,
                Benefits = goal.Benefits,
                TargetOn = goal.TargetOn,
                Today = _clock.Today,
                Context = goal.Context,
                HeldCredentials = Describe(credentials, _clock.Today)
            },
            CancellationToken.None);

        goal.Plan = LearningMapper.Serialize(result.Plan);
        goal.ResearchedAt = DateTime.UtcNow;
        goal.ResearchModel = result.Model;

        await _learningRepository.SavePlanAsync(goal, unitOfWork);

        return new DraftLearningPlanResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    /// <summary>
    /// Every credential held, whatever path it was earned for, so the draft does
    /// not spend a phase on something already on the wall. A lapsed one is still
    /// named — the useful advice there is "renew it", and the draft can only
    /// give that if it knows the thing exists.
    /// </summary>
    private static List<string> Describe(List<LearningCredential> credentials, DateOnly today) =>
        credentials
            .Select(credential =>
            {
                string name = string.IsNullOrWhiteSpace(credential.Code)
                    ? credential.Name
                    : $"{credential.Name} ({credential.Code})";

                string issuer = string.IsNullOrWhiteSpace(credential.Issuer) ? "" : $", {credential.Issuer}";

                string lapsed = credential.RenewalKind == CredentialRenewalKind.Expires &&
                                credential.ExpiresOn is { } expires &&
                                expires < today
                    ? " — lapsed, needs renewing"
                    : "";

                return $"{name}{issuer}, earned {credential.EarnedOn:yyyy-MM}{lapsed}";
            })
            .ToList();
}
