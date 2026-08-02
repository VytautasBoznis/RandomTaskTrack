using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Chat;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Chat;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class ChatController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public ChatController(OperationFactory operationFactory, ILogger<ChatController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] GetConversationsRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<GetConversationsOperation>().Run(request));
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var request = new GetConversationRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetConversationOperation>().Run(request));
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(SendChatMessageRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<SendChatMessageOperation>().Run(request));
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var request = new DeleteConversationRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteConversationOperation>().Run(request));
    }
}
