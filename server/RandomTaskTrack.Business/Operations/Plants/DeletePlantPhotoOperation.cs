using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Operations.Plants;

public class DeletePlantPhotoOperation : BaseOperation<DeletePlantPhotoRequest, DeletePlantPhotoResponse>
{
    private readonly IPlantsRepository _plantsRepository;

    public DeletePlantPhotoOperation(
        ILogger<DeletePlantPhotoOperation> logger,
        IValidator<DeletePlantPhotoRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeletePlantPhotoResponse> Execute(DeletePlantPhotoRequest request, IUnitOfWork unitOfWork)
    {
        return new DeletePlantPhotoResponse
        {
            Success = await _plantsRepository.DeletePhotoAsync(request.PhotoId, unitOfWork)
        };
    }
}
