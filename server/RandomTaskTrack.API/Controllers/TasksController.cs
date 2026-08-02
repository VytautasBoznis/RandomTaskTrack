using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Tasks;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Tasks;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class TasksController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public TasksController(OperationFactory operationFactory, ILogger<TasksController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    /// <summary>The single call the tablet dashboard makes.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] GetDashboardRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<GetDashboardOperation>().Run(request));
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetTasksRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<GetTasksOperation>().Run(request));
    }

    [HttpGet("completions")]
    public async Task<IActionResult> GetCompletionLog([FromQuery] GetCompletionLogRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<GetCompletionLogOperation>().Run(request));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateTaskOperation>().Run(request));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateTaskOperation>().Run(request));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteTaskRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CompleteTaskOperation>().Run(request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeleteTaskRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteTaskOperation>().Run(request));
    }
}
