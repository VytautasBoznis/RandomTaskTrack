using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Notes;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Notes;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class NotesController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public NotesController(OperationFactory operationFactory, ILogger<NotesController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var request = new GetNotesRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetNotesOperation>().Run(request));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateNoteRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateNoteOperation>().Run(request));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateNoteRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateNoteOperation>().Run(request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeleteNoteRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteNoteOperation>().Run(request));
    }
}
