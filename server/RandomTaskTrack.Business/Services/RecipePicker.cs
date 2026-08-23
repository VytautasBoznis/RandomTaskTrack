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

        // Everything ever pulled is excluded, which is the whole point: the
        // scope exists to cook things that have not been cooked.
        List<string> alreadySeen = await _recipesRepository.GetSeenExternalIdsAsync(_source.Name, unitOfWork);

        SourceRecipe pulled = await _source.PullAsync(family.Code, alreadySeen, cancellationToken)
                              ?? throw new NotFoundException(
                                  $"No new {family.Name} dish came back — every candidate has been cooked already. Try another family.",
                                  ExceptionCodes.RECIPE_NO_NEW_DISHES);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Source = _source.Name,
            ExternalId = pulled.ExternalId,
            FamilyId = family.Id,
            Title = pulled.Title,
            ImageUrl = pulled.ImageUrl,
            SourceUrl = pulled.SourceUrl,
            ReadyMinutes = pulled.ReadyMinutes,
            Servings = pulled.Servings,
            Ingredients = RecipeMapper.Serialize(pulled.Ingredients),
            Steps = RecipeMapper.Serialize(pulled.Steps)
        };

        await _recipesRepository.CreateRecipeAsync(recipe, unitOfWork);

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
        // the one pulled here stays in recipe_recipes so it is not offered again.
        _logger.LogInformation("Week of {WeekOf} already had a dish; keeping it", weekOf);

        return await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork)
               ?? throw new NotFoundException("The week's dish disappeared mid-pick.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);
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
