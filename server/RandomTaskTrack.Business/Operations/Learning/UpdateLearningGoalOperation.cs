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
/// Edits what the user owns. The drafted plan is deliberately not touched here:
/// it has its own endpoint, and the two have different owners in the same way a
/// plant's name and its care profile do.
/// </summary>
public class UpdateLearningGoalOperation : BaseOperation<UpdateLearningGoalRequest, UpdateLearningGoalResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public UpdateLearningGoalOperation(
        ILogger<UpdateLearningGoalOperation> logger,
        IValidator<UpdateLearningGoalRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<UpdateLearningGoalResponse> Execute(UpdateLearningGoalRequest request, IUnitOfWork unitOfWork)
    {
        LearningGoal goal = await _learningRepository.GetGoalAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning goal with id {request.Id}", ExceptionCodes.LEARNING_GOAL_NOT_FOUND);

        goal.Title = request.Title.Trim();
        goal.Tier = request.Tier;
        goal.Status = request.Status;
        goal.Why = request.Why?.Trim() ?? "";
        goal.Benefits = request.Benefits?.Trim() ?? "";
        goal.TargetOn = request.TargetOn;
        goal.Context = request.Context?.Trim() ?? "";
        goal.Notes = request.Notes?.Trim() ?? "";

        await _learningRepository.UpdateGoalAsync(goal, unitOfWork);

        return new UpdateLearningGoalResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }
}
