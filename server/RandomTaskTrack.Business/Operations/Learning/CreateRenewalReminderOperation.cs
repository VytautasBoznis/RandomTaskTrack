using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Puts a renewal on the board, once, on a date.
///
/// This is the whole of "the app chases you about renewing", and it is
/// deliberately a press rather than a background sweep. Expiry is *derived*
/// from expires_on and shown on the card; only a task is stored. The reverse —
/// materializing reminders automatically — would need a rule for what happens
/// when a credential is renewed early, and the honest answer is that the old
/// reminder is wrong and has to be found and removed, which is machinery for a
/// handful of rows a person looks at anyway.
///
/// One-off and dated, never a recurrence: renewing moves the next date, so a
/// yearly recurrence would drift off the real expiry within one cycle.
/// </summary>
public class CreateRenewalReminderOperation : BaseOperation<CreateRenewalReminderRequest, CreateRenewalReminderResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IClock _clock;

    public CreateRenewalReminderOperation(
        ILogger<CreateRenewalReminderOperation> logger,
        IValidator<CreateRenewalReminderRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateRenewalReminderResponse> Execute(CreateRenewalReminderRequest request, IUnitOfWork unitOfWork)
    {
        LearningCredential credential = await _learningRepository.GetCredentialAsync(request.Id, unitOfWork)
                                        ?? throw new NotFoundException($"No credential with id {request.Id}", ExceptionCodes.LEARNING_CREDENTIAL_NOT_FOUND);

        Guard(credential);

        List<TaskListItemDto> existing = await _learningRepository.GetCredentialTasksAsync([credential.Id], unitOfWork);

        // Already on the board. A second press is a double-tap.
        if (existing.Count == 0)
        {
            TaskDomain domain = await _domainsRepository.GetByCodeAsync(DomainCodes.Learning, unitOfWork)
                                ?? throw new NotFoundException($"No '{DomainCodes.Learning}' domain to file the renewal under.", ExceptionCodes.DOMAIN_NOT_FOUND);

            await _tasksRepository.CreateAsync(new TaskItem
            {
                Id = Guid.NewGuid(),
                DomainId = domain.Id,
                Title = $"Renew {Describe(credential)}",
                Notes = BuildNotes(credential),
                Data = LearningMapper.PayloadForCredential(credential.Id),
                DueOn = request.DueOn ?? DefaultDueOn(credential),
                Status = TaskItemStatus.Pending
            }, unitOfWork);
        }

        return new CreateRenewalReminderResponse
        {
            Credential = await LearningLoader.LoadCredentialAsync(credential.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    /// <summary>
    /// Refused rather than quietly scheduled for nothing. A reminder to renew
    /// something permanent is noise on the dashboard forever, and a reminder for
    /// something nobody has dated has no date to use.
    /// </summary>
    private static void Guard(LearningCredential credential)
    {
        if (credential.RenewalKind == CredentialRenewalKind.Permanent)
        {
            throw new BadRequestException(
                $"{credential.Name} does not expire, so there is nothing to renew.",
                ExceptionCodes.LEARNING_CREDENTIAL_PERMANENT);
        }

        // Distinct from the permanent case: this one is answerable, and the
        // description says how. Sharing a code would tell the UI they are the
        // same situation when one is finished and the other is a job.
        if (credential.ExpiresOn is null)
        {
            throw new BadRequestException(
                $"{credential.Name} has no expiry date yet.",
                ExceptionCodes.LEARNING_CREDENTIAL_NOT_DATED,
                "Look up how it renews, or set the expiry date by hand.");
        }
    }

    /// <summary>
    /// The day the renewal window opens, from what the lookup found — the
    /// windows genuinely differ, and Microsoft's six months is nothing like the
    /// few weeks some issuers give.
    ///
    /// Never in the past: for a credential already inside its window the date
    /// that matters is today, and back-dating it would land the reminder on the
    /// dashboard pre-aged by months it was never actually late by.
    /// </summary>
    private DateOnly DefaultDueOn(LearningCredential credential)
    {
        CredentialRenewal? renewal = LearningMapper.DeserializeRenewal(credential.Renewal);

        int window = renewal?.WindowOpensDays > 0
            ? renewal.WindowOpensDays
            : LearningMapper.DefaultRenewalWindowDays;

        DateOnly opens = credential.ExpiresOn!.Value.AddDays(-window);

        return opens < _clock.Today ? _clock.Today : opens;
    }

    private static string Describe(LearningCredential credential) =>
        string.IsNullOrWhiteSpace(credential.Code)
            ? credential.Name
            : $"{credential.Name} ({credential.Code})";

    /// <summary>
    /// The expiry date and how renewing works, so the task on the dashboard
    /// answers "what do I actually do" without a trip back to this tab.
    /// </summary>
    private static string BuildNotes(LearningCredential credential)
    {
        string expiry = $"Expires {credential.ExpiresOn:yyyy-MM-dd}.";
        string how = LearningMapper.DeserializeRenewal(credential.Renewal)?.Renewal ?? "";

        return string.IsNullOrWhiteSpace(how) ? expiry : $"{expiry} {how}";
    }
}
