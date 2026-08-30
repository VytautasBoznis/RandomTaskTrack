using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Learning;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Learning;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class LearningController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public LearningController(OperationFactory operationFactory, ILogger<LearningController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    /// <summary>Every path with its steps, and every credential held.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var request = new GetLearningRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetLearningOperation>().Run(request));
    }

    // ── Goals ────────────────────────────────────────────────────────────────

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal(CreateLearningGoalRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateLearningGoalOperation>().Run(request));
    }

    [HttpPut("goals/{id:guid}")]
    public async Task<IActionResult> UpdateGoal(Guid id, UpdateLearningGoalRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateLearningGoalOperation>().Run(request));
    }

    /// <summary>Takes the steps and their pending tasks with it. Credentials stay.</summary>
    [HttpDelete("goals/{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        var request = new DeleteLearningGoalRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteLearningGoalOperation>().Run(request));
    }

    /// <summary>Drafts the route, or drafts it again. Committed steps survive it.</summary>
    [HttpPost("goals/{id:guid}/plan")]
    public async Task<IActionResult> DraftPlan(Guid id, DraftLearningPlanRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<DraftLearningPlanOperation>().Run(request));
    }

    /// <summary>Commits chosen lines of the plan to the path.</summary>
    [HttpPost("goals/{id:guid}/steps")]
    public async Task<IActionResult> CreateSteps(Guid id, CreateLearningStepsRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateLearningStepsOperation>().Run(request));
    }

    // ── Steps ────────────────────────────────────────────────────────────────

    /// <summary>Status, dates, and the outcome — the grade or the retake.</summary>
    [HttpPut("steps/{id:guid}")]
    public async Task<IActionResult> UpdateStep(Guid id, UpdateLearningStepRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateLearningStepOperation>().Run(request));
    }

    [HttpDelete("steps/{id:guid}")]
    public async Task<IActionResult> DeleteStep(Guid id)
    {
        var request = new DeleteLearningStepRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteLearningStepOperation>().Run(request));
    }

    /// <summary>Puts the step on the board as a dated task.</summary>
    [HttpPost("steps/{id:guid}/task")]
    public async Task<IActionResult> CreateStepTask(Guid id, CreateLearningStepTaskRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateLearningStepTaskOperation>().Run(request));
    }

    // ── Credentials ──────────────────────────────────────────────────────────

    [HttpPost("credentials")]
    public async Task<IActionResult> CreateCredential(CreateCredentialRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateCredentialOperation>().Run(request));
    }

    /// <summary>Also how a renewal is recorded — the same row, moved forward.</summary>
    [HttpPut("credentials/{id:guid}")]
    public async Task<IActionResult> UpdateCredential(Guid id, UpdateCredentialRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateCredentialOperation>().Run(request));
    }

    [HttpDelete("credentials/{id:guid}")]
    public async Task<IActionResult> DeleteCredential(Guid id)
    {
        var request = new DeleteCredentialRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteCredentialOperation>().Run(request));
    }

    /// <summary>Looks up whether it expires and how it renews. Never overwrites
    /// an answer already given by hand.</summary>
    [HttpPost("credentials/{id:guid}/renewal")]
    public async Task<IActionResult> ResearchCredential(Guid id)
    {
        var request = new ResearchCredentialRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<ResearchCredentialOperation>().Run(request));
    }

    /// <summary>Puts a dated renewal on the board. Rejected for a permanent one.</summary>
    [HttpPost("credentials/{id:guid}/reminder")]
    public async Task<IActionResult> CreateReminder(Guid id, CreateRenewalReminderRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateRenewalReminderOperation>().Run(request));
    }
}
