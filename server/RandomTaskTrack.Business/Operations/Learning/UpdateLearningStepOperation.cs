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
/// Advances a step, or records what came of it. Both are this one endpoint: an
/// exam that was sat and failed is a status and an outcome written in the same
/// gesture, and splitting them would mean two round trips to say one thing.
/// </summary>
public class UpdateLearningStepOperation : BaseOperation<UpdateLearningStepRequest, UpdateLearningStepResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public UpdateLearningStepOperation(
        ILogger<UpdateLearningStepOperation> logger,
        IValidator<UpdateLearningStepRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<UpdateLearningStepResponse> Execute(UpdateLearningStepRequest request, IUnitOfWork unitOfWork)
    {
        LearningStep step = await _learningRepository.GetStepAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning step with id {request.Id}", ExceptionCodes.LEARNING_STEP_NOT_FOUND);

        step.Title = request.Title.Trim();
        step.Kind = request.Kind;
        step.Status = request.Status;
        step.TargetOn = request.TargetOn;
        step.Notes = request.Notes?.Trim() ?? "";
        step.Outcome = request.Outcome?.Trim() ?? "";
        step.Provider = Clean(request.Provider);
        step.Url = Clean(request.Url);
        step.Cost = Clean(request.Cost);
        step.Hours = request.Hours;
        step.SortOrder = request.SortOrder;

        await _learningRepository.UpdateStepAsync(step, unitOfWork);

        return new UpdateLearningStepResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(step.GoalId, _clock.Today, _learningRepository, unitOfWork)
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
