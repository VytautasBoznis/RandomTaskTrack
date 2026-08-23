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
}
