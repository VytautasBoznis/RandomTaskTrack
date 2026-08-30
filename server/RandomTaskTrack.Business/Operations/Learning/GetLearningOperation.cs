using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Dtos.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// The whole tab in one round trip: every path with its steps and the tasks
/// those steps have on the board, plus every credential held. Five queries
/// rather than five per goal — the steps and tasks are fetched for all of them
/// at once and grouped here.
/// </summary>
public class GetLearningOperation : BaseOperation<GetLearningRequest, GetLearningResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public GetLearningOperation(
        ILogger<GetLearningOperation> logger,
        IValidator<GetLearningRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<GetLearningResponse> Execute(GetLearningRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly today = _clock.Today;

        List<LearningGoal> goals = await _learningRepository.GetGoalsAsync(unitOfWork);
        List<LearningCredential> credentials = await _learningRepository.GetCredentialsAsync(unitOfWork);

        List<LearningStep> steps = goals.Count == 0
            ? new List<LearningStep>()
            : await _learningRepository.GetStepsAsync(goals.Select(goal => goal.Id), unitOfWork);

        List<LearningStepDto> stepDtos =
            await LearningLoader.LoadStepsAsync(steps, _learningRepository, unitOfWork);

        Dictionary<Guid, List<LearningStepDto>> stepsByGoal = stepDtos
            .GroupBy(step => step.GoalId)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<TaskListItemDto> reminders = credentials.Count == 0
            ? new List<TaskListItemDto>()
            : await _learningRepository.GetCredentialTasksAsync(credentials.Select(c => c.Id), unitOfWork);

        Dictionary<Guid, TaskListItemDto> remindersByCredential = reminders
            .Select(task => (CredentialId: LearningMapper.CredentialIdOf(task.Data), Task: task))
            .Where(pair => pair.CredentialId.HasValue)
            .GroupBy(pair => pair.CredentialId!.Value)
            .ToDictionary(group => group.Key, group => group.First().Task);

        return new GetLearningResponse
        {
            Goals = goals
                .Select(goal => LearningMapper.ToDto(
                    goal,
                    stepsByGoal.GetValueOrDefault(goal.Id) ?? new List<LearningStepDto>(),
                    today))
                .ToList(),

            Credentials = credentials
                .Select(credential => LearningMapper.ToDto(
                    credential,
                    remindersByCredential.GetValueOrDefault(credential.Id),
                    today))
                .ToList()
        };
    }
}
