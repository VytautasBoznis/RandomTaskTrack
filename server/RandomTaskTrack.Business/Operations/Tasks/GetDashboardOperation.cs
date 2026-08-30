using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Learning;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class GetDashboardOperation : BaseOperation<GetDashboardRequest, GetDashboardResponse>
{
    private readonly ITasksRepository _tasksRepository;
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public GetDashboardOperation(
        ILogger<GetDashboardOperation> logger,
        IValidator<GetDashboardRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<GetDashboardResponse> Execute(GetDashboardRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly today = _clock.Today;

        List<TaskListItemDto> overdue = await _tasksRepository.GetOverdueAsync(today, unitOfWork);
        List<TaskListItemDto> dueToday = await _tasksRepository.GetDueOnAsync(today, unitOfWork);
        List<TaskListItemDto> upcoming = await _tasksRepository.GetUpcomingAsync(today, today.AddDays(request.UpcomingDays), unitOfWork);

        var dashboard = new DashboardDto
        {
            Today = today,

            // The three pending buckets lose their learning rows to the section
            // below. Done today keeps its: a finished step is the reward for
            // having done it, it is a single line, and it clears overnight —
            // none of which is true of the pending ones that piled up.
            Overdue = WithoutLearning(overdue),
            DueToday = WithoutLearning(dueToday),
            Upcoming = WithoutLearning(upcoming),
            CompletedToday = await _tasksRepository.GetCompletedOnAsync(today, unitOfWork),

            Learning = await BuildLearningAsync([.. overdue, .. dueToday, .. upcoming], unitOfWork),

            Streaks = await _tasksRepository.GetDomainStreaksAsync(today, unitOfWork)
        };

        return new GetDashboardResponse { Dashboard = dashboard };
    }

    private static List<TaskListItemDto> WithoutLearning(List<TaskListItemDto> tasks) =>
        tasks.Where(task => task.DomainCode != DomainCodes.Learning).ToList();

    /// <summary>
    /// The learning rows, grouped and dated from the tasks already read for the
    /// buckets. The titles are the only thing that has to be fetched: a task
    /// payload names the path it came from but not what that path is called,
    /// and both tables are small enough to read whole — the learning tab does
    /// exactly the same on every load.
    /// </summary>
    private async Task<List<DashboardLearningDto>> BuildLearningAsync(List<TaskListItemDto> pending, IUnitOfWork unitOfWork)
    {
        List<TaskListItemDto> learning = pending.Where(task => task.DomainCode == DomainCodes.Learning).ToList();

        if (learning.Count == 0)
        {
            return [];
        }

        List<LearningGoal> goals = await _learningRepository.GetGoalsAsync(unitOfWork);
        List<LearningCredential> credentials = await _learningRepository.GetCredentialsAsync(unitOfWork);

        Dictionary<Guid, string> paths = goals.ToDictionary(goal => goal.Id, goal => goal.Title);
        Dictionary<Guid, string> held = credentials.ToDictionary(credential => credential.Id, credential => credential.Name);

        // A step's tasks collapse into the path they sit on.
        IEnumerable<DashboardLearningDto> steps = learning
            .Select(task => (GoalId: LearningMapper.GoalIdOf(task.Data), Task: task))
            .Where(pair => pair.GoalId is { } id && paths.ContainsKey(id))
            .GroupBy(pair => pair.GoalId!.Value)
            .Select(path => new DashboardLearningDto
            {
                GoalId = path.Key,
                Title = paths[path.Key],
                Count = path.Count(),
                NextDueOn = path.Min(pair => pair.Task.DueOn)
            });

        // Renewals sit on no path. Rolled together they would hide which
        // credential is the one about to lapse, so they stay one row each.
        IEnumerable<DashboardLearningDto> renewals = learning
            .Select(task => (CredentialId: LearningMapper.CredentialIdOf(task.Data), Task: task))
            .Where(pair => pair.CredentialId is { } id && held.ContainsKey(id))
            .GroupBy(pair => pair.CredentialId!.Value)
            .Select(credential => new DashboardLearningDto
            {
                Title = $"Renew {held[credential.Key]}",
                Count = credential.Count(),
                NextDueOn = credential.Min(pair => pair.Task.DueOn)
            });

        return steps.Concat(renewals).OrderBy(row => row.NextDueOn).ToList();
    }
}
