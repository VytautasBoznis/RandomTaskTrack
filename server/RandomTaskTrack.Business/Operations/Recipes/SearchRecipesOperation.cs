using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Recipes;
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
        int pageSize = request.Number ?? _options.CandidatesPerPull;

        // One more than the page, then dropped. A count over the catalog's two
        // million rows would answer the same question — "is there a next page" —
        // for far more work, and the extra row is free on a LIMIT that is
        // already walking the index.
        List<SourceRecipe> candidates = await _source.SearchAsync(
            request.Query.Trim(),
            pageSize + 1,
            request.Offset,
            CancellationToken.None);

        bool hasMore = candidates.Count > pageSize;

        return new SearchRecipesResponse
        {
            Candidates = hasMore ? candidates.Take(pageSize).ToList() : candidates,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }
}
