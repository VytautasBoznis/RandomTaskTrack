using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// This week's dish, pulling one on first open if the week has none. Doing it
/// lazily rather than on a timer means the weekly dish is always there when the
/// tab is opened, and no quota is spent in weeks the tab is never opened.
/// </summary>
public class GetWeeklyDishOperation : BaseOperation<GetWeeklyDishRequest, GetWeeklyDishResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IRecipePicker _picker;
    private readonly IClock _clock;

    public GetWeeklyDishOperation(
        ILogger<GetWeeklyDishOperation> logger,
        IValidator<GetWeeklyDishRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IRecipePicker picker,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _picker = picker;
        _clock = clock;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<GetWeeklyDishResponse> Execute(GetWeeklyDishRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly weekOf = RecipeMapper.MondayOf(_clock.Today);

        List<RecipeFamily> families = await _recipesRepository.GetFamiliesAsync(unitOfWork);

        RecipePick pick = await _recipesRepository.GetCurrentPickAsync(weekOf, unitOfWork)
                          ?? await _picker.PickAsync(weekOf, null, unitOfWork, CancellationToken.None);

        Recipe recipe = await _recipesRepository.GetRecipeAsync(pick.RecipeId, unitOfWork)
                        ?? throw new NotFoundException("The picked dish is missing.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);

        return new GetWeeklyDishResponse
        {
            Dish = RecipeMapper.ToDish(pick, recipe, families.FirstOrDefault(f => f.Id == recipe.FamilyId)?.Name),
            Families = families
        };
    }
}
