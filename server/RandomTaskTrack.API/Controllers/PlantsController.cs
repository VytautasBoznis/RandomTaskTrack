using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Plants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Response.Plants;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class PlantsController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public PlantsController(OperationFactory operationFactory, ILogger<PlantsController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    /// <summary>Every plant, with its care schedule and its pending tasks.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var request = new GetPlantsRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetPlantsOperation>().Run(request));
    }

    /// <summary>Adds a plant and looks it up. Saves either way — see the response.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreatePlantRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreatePlantOperation>().Run(request));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePlantRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdatePlantOperation>().Run(request));
    }

    /// <summary>Asks again, optionally with a better description.</summary>
    [HttpPost("{id:guid}/research")]
    public async Task<IActionResult> Research(Guid id, ResearchPlantRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<ResearchPlantOperation>().Run(request));
    }

    /// <summary>
    /// Adds a photo, and with it a stage. The AI is asked what it shows unless
    /// a stage is given.
    /// </summary>
    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> AddPhoto(Guid id, AddPlantPhotoRequest request)
    {
        request.PlantId = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<AddPlantPhotoOperation>().Run(request));
    }

    /// <summary>
    /// The image itself. Not an envelope — this is what an img tag loads, once
    /// the UI has fetched it with the bearer token like every other call.
    /// </summary>
    [HttpGet("photos/{photoId:guid}")]
    public async Task<IActionResult> GetPhoto(Guid photoId)
    {
        var request = new GetPlantPhotoRequest { PhotoId = photoId, SessionUserData = GetSessionModelFromJwt() };

        GetPlantPhotoResponse response = await _operationFactory.Get<GetPlantPhotoOperation>().Run(request);

        return File(response.Image, response.MediaType);
    }

    [HttpDelete("photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId)
    {
        var request = new DeletePlantPhotoRequest { PhotoId = photoId, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeletePlantPhotoOperation>().Run(request));
    }

    /// <summary>Dates a seed packet's plan from the day it actually gets sown.</summary>
    [HttpPost("{id:guid}/sowing")]
    public async Task<IActionResult> Sowing(Guid id, CreateSowingPlanRequest request)
    {
        request.PlantId = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateSowingPlanOperation>().Run(request));
    }

    /// <summary>Puts the chosen care tasks on the board as recurrences.</summary>
    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, CreatePlantScheduleRequest request)
    {
        request.PlantId = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreatePlantScheduleOperation>().Run(request));
    }

    /// <summary>Takes the plant, its schedules and its pending tasks with it.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeletePlantRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeletePlantOperation>().Run(request));
    }
}
