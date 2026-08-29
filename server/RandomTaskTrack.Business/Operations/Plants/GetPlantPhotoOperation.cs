using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>The only operation that returns bytes. The controller turns it into
/// a file result rather than an envelope.</summary>
public class GetPlantPhotoOperation : BaseOperation<GetPlantPhotoRequest, GetPlantPhotoResponse>
{
    private readonly IPlantsRepository _plantsRepository;

    public GetPlantPhotoOperation(
        ILogger<GetPlantPhotoOperation> logger,
        IValidator<GetPlantPhotoRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
    }

    protected override async Task<GetPlantPhotoResponse> Execute(GetPlantPhotoRequest request, IUnitOfWork unitOfWork)
    {
        PlantPhoto photo = await _plantsRepository.GetPhotoAsync(request.PhotoId, unitOfWork)
                           ?? throw new NotFoundException($"No photo with id {request.PhotoId}", ExceptionCodes.PLANT_NOT_FOUND);

        return new GetPlantPhotoResponse
        {
            Image = photo.Image,
            MediaType = photo.MediaType
        };
    }
}
