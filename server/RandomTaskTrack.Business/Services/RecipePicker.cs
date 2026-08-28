using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Services;

public class RecipePicker : IRecipePicker
{
    private readonly IRecipeSource _source;
    private readonly IRecipesRepository _recipesRepository;
    private readonly ILogger<RecipePicker> _logger;

    public RecipePicker(
        IRecipeSource source,
        IRecipesRepository recipesRepository,
        ILogger<RecipePicker> logger)
    {
        _source = source;
        _recipesRepository = recipesRepository;
        _logger = logger;
    }

    public async Task<RecipePick> PickAsync(DateOnly weekOf, int? familyId, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        RecipeFamily family = await ResolveFamilyAsync(familyId, unitOfWork);

        Recipe recipe = await TakeFromPoolAsync(family, weekOf, unitOfWork, cancellationToken);

        var pick = new RecipePick
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            WeekOf = weekOf,
            Status = RecipePickStatus.Current
        };

        if (await _recipesRepository.TryCreatePickAsync(pick, unitOfWork))
        {
            _logger.LogInformation("Picked {Title} ({Family}) for week of {WeekOf}", recipe.Title, family.Name, weekOf);

            return pick;
        }

        // Two tabs opened at once. The other request's dish is just as good, and
        // the one taken here never got a pick row, so it stays in the pool for
        // next week rather than being burnt.
        _logger.LogInformation("Week of {WeekOf} already had a dish; keeping it", weekOf);

        return await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork)
               ?? throw new NotFoundException("The week's dish disappeared mid-pick.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);
    }

    /// <summary>
    /// The library first, the source only when it has nothing. A pull banks
    /// every candidate it got, so the call that finds one dish also stocks the
    /// next nine — which is what keeps rerolling free and stops the pool running
    /// dry after a handful of weeks.
    /// </summary>
    private async Task<Recipe> TakeFromPoolAsync(RecipeFamily family, DateOnly weekOf, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        Recipe? banked = await _recipesRepository.GetPoolRecipeAsync(family.Id, weekOf, unitOfWork);

        if (banked is not null)
        {
            return banked;
        }

        List<SourceRecipe> pulled = await _source.PullAsync(family.Code, cancellationToken);

        _logger.LogInformation("Pool was empty for {Family}; banked {Count} dishes from {Source}", family.Name, pulled.Count, _source.Name);

        if (pulled.Count > 0)
        {
            await _recipesRepository.SaveRecipesAsync(
                pulled.Select(dish => RecipeMapper.ToRecipe(dish, _source.Name, family.Id)).ToList(),
                unitOfWork);
        }

        // Re-read rather than picking from `pulled`: some of those dishes were
        // already in the library and the insert skipped them, and the pool query
        // is the one place that knows which are still fair game.
        return await _recipesRepository.GetPoolRecipeAsync(family.Id, weekOf, unitOfWork)
               ?? throw new NotFoundException(
                   $"No new {family.Name} dish came back — every candidate has been cooked already. " +
                   "Try another family, or search for a dish by name.",
                   ExceptionCodes.RECIPE_NO_NEW_DISHES);
    }

    private async Task<RecipeFamily> ResolveFamilyAsync(int? familyId, IUnitOfWork unitOfWork)
    {
        if (familyId.HasValue)
        {
            return await _recipesRepository.GetFamilyByIdAsync(familyId.Value, unitOfWork)
                   ?? throw new NotFoundException($"No cuisine family with id {familyId}", ExceptionCodes.RECIPE_FAMILY_NOT_FOUND);
        }

        return await _recipesRepository.GetLeastRecentlyUsedFamilyAsync(unitOfWork)
               ?? throw new NotFoundException(
                   "There are no active cuisine families — seed tracker.recipe_families or reactivate one.",
                   ExceptionCodes.RECIPE_NO_FAMILIES);
    }
}
