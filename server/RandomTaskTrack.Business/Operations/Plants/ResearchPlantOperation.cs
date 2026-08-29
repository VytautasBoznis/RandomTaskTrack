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
using RandomTaskTrack.Interfaces.Plants;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Operations.Plants;

/// <summary>
/// Asks again — after a failed lookup, or with a better description once the
/// thing has flowered and given itself away.
///
/// Unlike the create path this reports failure as failure: the user pressed a
/// button that does exactly one thing, so silently doing nothing would be a lie.
/// </summary>
public class ResearchPlantOperation : BaseOperation<ResearchPlantRequest, ResearchPlantResponse>
{
    private readonly IPlantsRepository _plantsRepository;
    private readonly IPlantResearcher _researcher;

    public ResearchPlantOperation(
        ILogger<ResearchPlantOperation> logger,
        IValidator<ResearchPlantRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IPlantsRepository plantsRepository,
        IPlantResearcher researcher) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _plantsRepository = plantsRepository;
        _researcher = researcher;
    }

    /// <summary>One UPDATE behind a network call. See CreatePlantOperation.</summary>
    protected override bool RequiresTransaction => false;

    protected override async Task<ResearchPlantResponse> Execute(ResearchPlantRequest request, IUnitOfWork unitOfWork)
    {
        Plant plant = await _plantsRepository.GetByIdAsync(request.Id, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {request.Id}", ExceptionCodes.PLANT_NOT_FOUND);

        // A new description replaces the stored one: it is what the next
        // re-lookup should ask, and keeping the old one would make the profile
        // untraceable to the question that produced it.
        if (request.Description is not null)
        {
            plant.Description = request.Description.Trim();
        }

        // The newest photo is the plant as it looks now, which is the best thing
        // to be asking about — a seedling identified from its packet looks like
        // something else entirely three weeks later.
        PlantPhoto? photo = request.UsePhoto
            ? await _plantsRepository.GetLatestPhotoAsync(plant.Id, unitOfWork)
            : null;

        PlantResearchResult research = await _researcher.ResearchAsync(
            new PlantResearchQuestion
            {
                Name = plant.Name,
                Location = plant.Location,
                Description = plant.Description,
                Kind = plant.Kind,
                Image = photo is null ? null : PlantMapper.ToAiImage(photo)
            },
            CancellationToken.None);

        plant.Profile = PlantMapper.Serialize(research.Profile);
        plant.ResearchedAt = DateTime.UtcNow;
        plant.ResearchModel = research.Model;

        // The lookup only names the species when the user has not. A hand-typed
        // correction is the better answer by definition — it came from someone
        // holding the plant.
        plant.Species = string.IsNullOrWhiteSpace(plant.Species) ? research.Profile.SpeciesCommon : plant.Species;
        plant.LatinName = string.IsNullOrWhiteSpace(plant.LatinName) ? research.Profile.SpeciesLatin : plant.LatinName;

        await _plantsRepository.SaveProfileAsync(plant, unitOfWork);

        return new ResearchPlantResponse
        {
            Plant = await PlantLoader.LoadAsync(plant.Id, _plantsRepository, unitOfWork)
        };
    }
}
