using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Recipes;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Recipes;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class RecipesController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public RecipesController(OperationFactory operationFactory, ILogger<RecipesController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    /// <summary>This week's dish. Pulls one if the week does not have it yet.</summary>
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly()
    {
        var request = new GetWeeklyDishRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetWeeklyDishOperation>().Run(request));
    }

    [HttpPost("reroll")]
    public async Task<IActionResult> Reroll(RerollDishRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<RerollDishOperation>().Run(request));
    }

    [HttpPost("task")]
    public async Task<IActionResult> CreateTask(CreateDishTaskRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateDishTaskOperation>().Run(request));
    }

    /// <summary>Free-text search against the source. Saves nothing.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int? number, [FromQuery] int? offset)
    {
        var request = new SearchRecipesRequest
        {
            Query = query ?? "",
            Number = number,
            Offset = offset ?? 0,
            SessionUserData = GetSessionModelFromJwt()
        };

        return Ok(await _operationFactory.Get<SearchRecipesOperation>().Run(request));
    }

    /// <summary>Banks the search results worth keeping.</summary>
    [HttpPost("library")]
    public async Task<IActionResult> SaveToLibrary(SaveRecipesRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<SaveRecipesOperation>().Run(request));
    }

    /// <summary>Makes a library dish this week's, instead of drawing one.</summary>
    [HttpPost("pick")]
    public async Task<IActionResult> Pick(SetWeeklyDishRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<SetWeeklyDishOperation>().Run(request));
    }

    /// <summary>The cookbook — cooked dishes and the pool, filtered.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] string? search, [FromQuery] string? tags, [FromQuery] bool? cooked)
    {
        var request = new GetRecipeHistoryRequest
        {
            Search = search,
            // Comma-separated in the query string, the same way the tag box in
            // the UI takes them.
            Tags = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            Cooked = cooked,
            SessionUserData = GetSessionModelFromJwt()
        };

        return Ok(await _operationFactory.Get<GetRecipeHistoryOperation>().Run(request));
    }

    /// <summary>Takes this week's dish off the board without picking another.</summary>
    [HttpDelete("pick")]
    public async Task<IActionResult> ClearPick()
    {
        var request = new ClearWeeklyDishRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<ClearWeeklyDishOperation>().Run(request));
    }

    /// <summary>How much of the bulk catalog is loaded, and whether a run is going.</summary>
    [HttpGet("catalog")]
    public async Task<IActionResult> CatalogStatus()
    {
        var request = new GetCatalogStatusRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetCatalogStatusOperation>().Run(request));
    }

    /// <summary>Starts the bulk load in the background. Re-running adds only new dishes.</summary>
    [HttpPost("catalog/import")]
    public async Task<IActionResult> CatalogImport()
    {
        var request = new StartCatalogImportRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<StartCatalogImportOperation>().Run(request));
    }

    [HttpPut("{recipeId:guid}")]
    public async Task<IActionResult> Update(Guid recipeId, UpdateRecipeRequest request)
    {
        request.RecipeId = recipeId;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateRecipeOperation>().Run(request));
    }
}
