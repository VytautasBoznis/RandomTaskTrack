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

public class CreateLearningGoalOperation : BaseOperation<CreateLearningGoalRequest, CreateLearningGoalResponse>
{
    private readonly ILearningRepository _learningRepository;
    private readonly IClock _clock;

    public CreateLearningGoalOperation(
        ILogger<CreateLearningGoalOperation> logger,
        IValidator<CreateLearningGoalRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ILearningRepository learningRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _learningRepository = learningRepository;
        _clock = clock;
    }

    protected override async Task<CreateLearningGoalResponse> Execute(CreateLearningGoalRequest request, IUnitOfWork unitOfWork)
    {
        var goal = new LearningGoal
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Tier = request.Tier,
            Why = request.Why?.Trim() ?? "",
            Benefits = request.Benefits?.Trim() ?? "",
            TargetOn = request.TargetOn,
            Context = request.Context?.Trim() ?? "",
            Notes = request.Notes?.Trim() ?? ""
        };

        await _learningRepository.CreateGoalAsync(goal, unitOfWork);

        return new CreateLearningGoalResponse
        {
            Goal = await LearningLoader.LoadGoalAsync(goal.Id, _clock.Today, _learningRepository, unitOfWork)
        };
    }
}
