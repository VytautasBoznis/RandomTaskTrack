using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// The cookbook: dishes already cooked, with the rating and notes left on them,
/// alongside dishes banked but not cooked yet. One list because they are the
/// same rows — whether a dish has been the weekly dish is a property of it, not
/// a different kind of thing.
/// </summary>
public class GetRecipeHistoryOperation : BaseOperation<GetRecipeHistoryRequest, GetRecipeHistoryResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly IClock _clock;

    public GetRecipeHistoryOperation(
        ILogger<GetRecipeHistoryOperation> logger,
        IValidator<GetRecipeHistoryRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _clock = clock;
    }

    protected override async Task<GetRecipeHistoryResponse> Execute(GetRecipeHistoryRequest request, IUnitOfWork unitOfWork)
    {
        return new GetRecipeHistoryResponse
        {
            Entries = await _recipesRepository.QueryHistoryAsync(
                request.Search,
                RecipeMapper.NormaliseTags(request.Tags),
                request.Cooked,
                RecipeMapper.MondayOf(_clock.Today),
                unitOfWork)
        };
    }
}
