using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Plants;
using RandomTaskTrack.Interfaces.Repositories.Plants;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// Adds a photo, which is how a stage gets recorded — the two are one act, so
/// there is no separate "mark a stage" anywhere.
///
/// The AI is asked what the photo shows, and its answer fills in the stage and
/// the note. That is best-effort: a photo is worth keeping whether or not
/// anything could be said about it, so a failed read comes back on the response
/// rather than losing the upload.
/// </summary>
public class AddPlantPhotoOperation : BaseOperation<AddPlantPhotoRequest, AddPlantPhotoResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly IPlantResearcher _researcher;
    private readonly IClock _clock;

    public AddPlantPhotoOperation(
        ILogger<AddPlantPhotoOperation> logger,
        IValidator<AddPlantPhotoRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository,
        IPlantResearcher researcher,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
        _researcher = researcher;
        _clock = clock;
    }

    /// <summary>Two statements around a network call. See CreatePlantOperation.</summary>
    protected override bool RequiresTransaction => false;

    protected override async Task<AddPlantPhotoResponse> Execute(AddPlantPhotoRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.PlantId, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.PlantId}", ExceptionCodes.PLANT_NOT_FOUND);

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            throw new BadRequestException(
                "The photo was not valid base64.",
                ExceptionCodes.VALIDATION_FAILED,
                "Send the raw base64 without a data: prefix.");
        }

        var photo = new PlantPhoto
        {
            Id = Guid.NewGuid(),
            PlantId = plant.Id,
            Image = bytes,
            MediaType = request.MediaType,
            Stage = request.Stage ?? "",
            Note = request.Note ?? "",
            TakenOn = request.TakenOn ?? _clock.Today
        };

        await _plantsRepository.AddPhotoAsync(photo, unitOfWork);

        string? readError = null;

        // A hand-typed stage is the user telling us what it is, so there is
        // nothing to ask. Only an unlabelled photo goes to the model.
        if (request.Stage is null)
        {
            try
            {
                var image = new AiImage { Base64 = request.ImageBase64, MediaType = request.MediaType };
                PlantStageRead read = await _researcher.ReadStageAsync(plant, image, CancellationToken.None);

                await _plantsRepository.SavePhotoReadAsync(
                    photo.Id,
                    read.Stage,
                    string.IsNullOrWhiteSpace(request.Note) ? read.Note : request.Note!,
                    unitOfWork);
            }
            catch (AiProviderException ex)
            {
                _logger.LogWarning(ex, "Stage read failed for plant {PlantId}; the photo is kept unlabelled", plant.Id);

                readError = ex.Message;
            }
        }

        return new AddPlantPhotoResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork),
            ReadError = readError
        };
    }
}
