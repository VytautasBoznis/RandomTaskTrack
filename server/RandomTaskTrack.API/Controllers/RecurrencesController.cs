using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Recurrences;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Recurrences;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class RecurrencesController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public RecurrencesController(OperationFactory operationFactory, ILogger<RecurrencesController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetRecurrencesRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<GetRecurrencesOperation>().Run(request));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRecurrenceRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateRecurrenceOperation>().Run(request));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRecurrenceRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateRecurrenceOperation>().Run(request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool deleteFutureTasks = true)
    {
        var request = new DeleteRecurrenceRequest
        {
            Id = id,
            DeleteFutureTasks = deleteFutureTasks,
            SessionUserData = GetSessionModelFromJwt()
        };

        return Ok(await _operationFactory.Get<DeleteRecurrenceOperation>().Run(request));
    }
}
