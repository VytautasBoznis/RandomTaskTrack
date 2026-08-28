using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Overriding the rotation: "not whatever is next, ramen". Deliberately writes
/// nothing — the point of the search screen is to choose which results are worth
/// keeping, so saving is a separate, explicit step.
/// </summary>
public class SearchRecipesOperation : BaseOperation<SearchRecipesRequest, SearchRecipesResponse>
{
    private readonly IRecipeSource _source;
    private readonly RecipeOptions _options;

    public SearchRecipesOperation(
        ILogger<SearchRecipesOperation> logger,
        IValidator<SearchRecipesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipeSource source,
        IOptions<RecipeOptions> options) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _source = source;
        _options = options.Value;
    }

    protected override async Task<SearchRecipesResponse> Execute(SearchRecipesRequest request, IUnitOfWork unitOfWork)
    {
        return new SearchRecipesResponse
        {
            Candidates = await _source.SearchAsync(
                request.Query.Trim(),
                request.Number ?? _options.CandidatesPerPull,
                CancellationToken.None)
        };
    }
}
