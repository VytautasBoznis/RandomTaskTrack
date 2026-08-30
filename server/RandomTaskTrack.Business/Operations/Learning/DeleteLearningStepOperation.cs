using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Data.Request.Learning;
using RandomTaskTrack.Data.Response.Learning;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Learning;

namespace RandomTaskTrack.Business.Operations.Learning;

/// <summary>
/// Takes the step and any pending task it put on the board. Nothing in the
/// database would do that — the link is a payload, not a foreign key — so it is
/// done here, the same way DeletePlantOperation sweeps up care tasks.
/// </summary>
public class DeleteLearningStepOperation : BaseOperation<DeleteLearningStepRequest, DeleteLearningStepResponse>
{
    private readonly ILearningRepository _learningRepository;

    public DeleteLearningStepOperation(
        ILogger<DeleteLearningStepOperation> logger,
        IValidator<DeleteLearningStepRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteLearningStepResponse> Execute(DeleteLearningStepRequest request, IUnitOfWork unitOfWork)
    {
        LearningStep step = await _learningRepository.GetStepAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException($"No learning step with id {request.Id}", ExceptionCodes.LEARNING_STEP_NOT_FOUND);

        int deletedTasks = await _learningRepository.DeleteStepTasksAsync([step.Id], unitOfWork);

        return new DeleteLearningStepResponse
        {
            Success = await _learningRepository.DeleteStepAsync(step.Id, unitOfWork),
            DeletedTaskCount = deletedTasks
        };
    }
}
