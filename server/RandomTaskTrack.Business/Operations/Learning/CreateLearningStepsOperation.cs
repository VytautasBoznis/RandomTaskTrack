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
/// Commits chosen lines of a drafted plan — or hand-typed ones — to a path.
///
/// Deduped by title, the way CreatePlantScheduleOperation dedupes care lines: a
/// tablet turns one press into two often enough to be worth guarding, and
/// pressing "add to path" on a resource that is already on it should be a
/// no-op rather than a second row. The response carries the whole goal back, so
/// nothing is hidden by the dedupe.
/// </summary>
public class CreateLearningStepsOperation : BaseOperation<CreateLearningStepsRequest, CreateLearningStepsResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public CreateLearningStepsOperation(
        ILogger<CreateLearningStepsOperation> logger,
        IValidator<CreateLearningStepsRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateLearningStepsResponse> Execute(CreateLearningStepsRequest request, IUnitOfWork unitOfWork)
    {
        LearningGoal goal = await _learningRepository.GetGoalAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {request.Id}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        List<LearningStep> existing = await _learningRepository.GetStepsAsync([goal.Id], unitOfWork);

        // Dropped steps count as taken: re-adding something already decided
        // against is exactly what the dedupe should stop.
        var titles = existing
            .Select(step => step.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int sortOrder = await _learningRepository.GetMaxStepSortOrderAsync(goal.Id, unitOfWork);
        int created = 0;

        foreach (LearningStepInput input in request.Steps)
        {
            string title = input.Title.Trim();

            if (!titles.Add(title))
            {
                continue;
            }

            await _learningRepository.CreateStepAsync(new LearningStep
            {
                Id = Guid.NewGuid(),
                GoalId = goal.Id,
                Title = title,
                Kind = input.Kind,
                TargetOn = input.TargetOn,
                Notes = input.Notes?.Trim() ?? "",
                Provider = Clean(input.Provider),
                Url = Clean(input.Url),
                Cost = Clean(input.Cost),
                Hours = input.Hours,

                // Appended in the order they were picked, below whatever is
                // already on the path.
                SortOrder = ++sortOrder
            }, unitOfWork);

            created++;
        }

        return new CreateLearningStepsResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork),
            CreatedStepCount = created
        };
    }

    /// <summary>
    /// Empty strings become null. The draft leaves fields empty rather than
    /// omitting them, and an empty url is not a url — storing "" would make the
    /// UI render a link to nowhere.
    /// </summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
