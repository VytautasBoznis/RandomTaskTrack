using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Enums;
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
/// Adds a plant and looks it up in the same breath.
///
/// The lookup is allowed to fail. Leaving the AI unconfigured is a supported
/// way to run this app, so a plant added without one is still a plant — it is
/// saved either way and the failure comes back on the response for the card to
/// offer a retry. Only an explicit "look it up" press treats it as an error.
/// </summary>
public class CreatePlantOperation : BaseOperation<CreatePlantRequest, CreatePlantResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly IPlantResearcher _researcher;
    private readonly IClock _clock;

    public CreatePlantOperation(
        ILogger<CreatePlantOperation> logger,
        IValidator<CreatePlantRequest> validator,
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

    /// <summary>
    /// Deliberately not transactional: the write is a single INSERT, and the
    /// lookup in front of it is a network call that can take ten seconds. A
    /// transaction here would be an idle one held open for the duration.
    /// </summary>
    protected override bool RequiresTransaction => false;

    protected override async Task<CreatePlantResponse> Execute(CreatePlantRequest request, IUnitOfWork unitOfWork)
    {
        var plant = new Plant
        {
            Id = Guid.NewGuid(),
            Kind = request.Kind,
            Name = request.Name.Trim(),
            Location = request.Location,
            AcquiredOn = request.AcquiredOn,
            Notes = request.Notes ?? "",
            Description = request.Description?.Trim() ?? ""
        };

        // The photo is both what the lookup looks at and the plant's first
        // stage, so it is decoded once and used for both.
        AiImage? image = string.IsNullOrEmpty(request.ImageBase64)
            ? null
            : new AiImage { Base64 = request.ImageBase64, MediaType = request.MediaType! };

        string? researchError = null;

        try
        {
            PlantResearchResult research = await _researcher.ResearchAsync(
                new PlantResearchQuestion
                {
                    Name = plant.Name,
                    Location = plant.Location,
                    Description = plant.Description,
                    Kind = plant.Kind,
                    Image = image
                },
                CancellationToken.None);

            plant.Profile = PlantMapper.Serialize(research.Profile);
            plant.Species = research.Profile.SpeciesCommon;
            plant.LatinName = research.Profile.SpeciesLatin;
            plant.ResearchedAt = DateTime.UtcNow;
            plant.ResearchModel = research.Model;
        }
        catch (AiProviderException ex)
        {
            _logger.LogWarning(ex, "Plant lookup failed for {Name}; saving it without a profile", plant.Name);

            researchError = ex.Message;
        }

        await _plantsRepository.CreateAsync(plant, unitOfWork);

        if (image is not null)
        {
            await _plantsRepository.AddPhotoAsync(new PlantPhoto
            {
                Id = Guid.NewGuid(),
                PlantId = plant.Id,
                Image = Convert.FromBase64String(image.Base64),
                MediaType = image.MediaType,

                // The identification already looked at this one, so it is not
                // sent a second time for a stage read. It is simply where the
                // plant started.
                Stage = plant.Kind == PlantKind.SeedPacket ? "Packet" : "As added",
                Note = "",
                TakenOn = _clock.Today
            }, unitOfWork);
        }

        return new CreatePlantResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork),
            ResearchError = researchError
        };
    }
}
