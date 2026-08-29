using RandomTaskTrack.Data.Dtos.Plants;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Plants;

namespace RandomTaskTrack.Business.Plants;

/// <summary>
/// Reads one plant back with its schedule and its tasks. Every write operation
/// ends this way, so the card the UI re-renders is the plant as it now stands
/// rather than the request echoed back.
/// </summary>
internal static class PlantLoader
{
    public static async Task<PlantDto> LoadAsync(Guid plantId, IPlantsRepository plantsRepository, IUnitOfWork unitOfWork)
    {
        Plant plant = await plantsRepository.GetByIdAsync(plantId, unitOfWork)
                      ?? throw new NotFoundException($"No plant with id {plantId}", ExceptionCodes.PLANT_NOT_FOUND);

        Guid[] ids = [plant.Id];

        return PlantMapper.ToDto(
            plant,
            await plantsRepository.GetPendingTasksAsync(ids, unitOfWork),
            await plantsRepository.GetCareRecurrencesAsync(ids, unitOfWork),
            await plantsRepository.GetPhotosAsync(ids, unitOfWork));
    }
}
