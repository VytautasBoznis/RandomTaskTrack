using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Operations.Plants;

public class UpdatePlantOperation : BaseOperation<UpdatePlantRequest, UpdatePlantResponse>
{
    private readonly IPlantsRepository _plantsRepository;

    public UpdatePlantOperation(
        ILogger<UpdatePlantOperation> logger,
        IValidator<UpdatePlantRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdatePlantResponse> Execute(UpdatePlantRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.Id, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.Id}", ExceptionCodes.PLANT_NOT_FOUND);

        // Null is "leave alone" throughout, so the form can send only what it
        // touched. The profile is not editable here at all — it belongs to the
        // lookup, and a hand-edited one would silently be overwritten by the
        // next re-research.
        plant.Kind = request.Kind ?? plant.Kind;
        plant.Name = request.Name ?? plant.Name;
        plant.Location = request.Location ?? plant.Location;
        plant.Species = request.Species ?? plant.Species;
        plant.LatinName = request.LatinName ?? plant.LatinName;
        plant.AcquiredOn = request.AcquiredOn ?? plant.AcquiredOn;
        plant.Notes = request.Notes ?? plant.Notes;

        await _plantsRepository.UpdateAsync(plant, unitOfWork);

        return new UpdatePlantResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork)
        };
    }
}
