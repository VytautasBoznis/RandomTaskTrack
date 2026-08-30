using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Finance;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Finance;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class FinanceController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public FinanceController(OperationFactory operationFactory, ILogger<FinanceController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    // ── Overview and projection ──────────────────────────────────────────────

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var request = new GetFinanceOverviewRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<GetFinanceOverviewOperation>().Run(request));
    }

    /// <param name="stockGrowth">
    /// Assumed annual return on holdings, as a percentage. Defaults to zero —
    /// hold at the last pulled price.
    /// </param>
    [HttpGet("projection")]
    public async Task<IActionResult> GetProjection(
        [FromQuery] int months = 60,
        [FromQuery] int historyMonths = 12,
        [FromQuery] decimal stockGrowth = 0)
    {
        var request = new GetProjectionRequest
        {
            Months = months,
            HistoryMonths = historyMonths,
            StockGrowthPct = stockGrowth,
            SessionUserData = GetSessionModelFromJwt()
        };

        return Ok(await _operationFactory.Get<GetProjectionOperation>().Run(request));
    }

    [HttpPost("prices/refresh")]
    public async Task<IActionResult> RefreshPrices()
    {
        var request = new RefreshPricesRequest { SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<RefreshPricesOperation>().Run(request));
    }

    // ── Accounts ─────────────────────────────────────────────────────────────

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateAccountOperation>().Run(request));
    }

    [HttpPut("accounts/{id:guid}")]
    public async Task<IActionResult> UpdateAccount(Guid id, UpdateAccountRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateAccountOperation>().Run(request));
    }

    [HttpDelete("accounts/{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var request = new DeleteAccountRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteAccountOperation>().Run(request));
    }

    /// <summary>
    /// Types the balance you can see and logs the difference. The balance
    /// itself is not stored — see <see cref="SetAccountBalanceOperation"/>.
    /// </summary>
    [HttpPost("accounts/{id:guid}/balance")]
    public async Task<IActionResult> SetAccountBalance(Guid id, SetAccountBalanceRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<SetAccountBalanceOperation>().Run(request));
    }

    // ── Flows ────────────────────────────────────────────────────────────────

    [HttpPost("flows")]
    public async Task<IActionResult> CreateFlow(CreateFlowRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateFlowOperation>().Run(request));
    }

    [HttpPut("flows/{id:guid}")]
    public async Task<IActionResult> UpdateFlow(Guid id, UpdateFlowRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateFlowOperation>().Run(request));
    }

    [HttpDelete("flows/{id:guid}")]
    public async Task<IActionResult> DeleteFlow(Guid id)
    {
        var request = new DeleteFlowRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteFlowOperation>().Run(request));
    }

    // ── Ledger ───────────────────────────────────────────────────────────────

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] FinanceFlowKind? kind,
        [FromQuery] string? search,
        [FromQuery] int limit = 200)
    {
        var request = new GetEntriesRequest
        {
            From = from,
            To = to,
            Kind = kind,
            Search = search,
            Limit = limit,
            SessionUserData = GetSessionModelFromJwt()
        };

        return Ok(await _operationFactory.Get<GetEntriesOperation>().Run(request));
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry(CreateEntryRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateEntryOperation>().Run(request));
    }

    [HttpPut("entries/{id:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid id, UpdateEntryRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateEntryOperation>().Run(request));
    }

    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var request = new DeleteEntryRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteEntryOperation>().Run(request));
    }

    // ── Holdings ─────────────────────────────────────────────────────────────

    [HttpPost("holdings")]
    public async Task<IActionResult> CreateHolding(CreateHoldingRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateHoldingOperation>().Run(request));
    }

    [HttpPut("holdings/{id:guid}")]
    public async Task<IActionResult> UpdateHolding(Guid id, UpdateHoldingRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateHoldingOperation>().Run(request));
    }

    [HttpDelete("holdings/{id:guid}")]
    public async Task<IActionResult> DeleteHolding(Guid id)
    {
        var request = new DeleteHoldingRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteHoldingOperation>().Run(request));
    }

    // ── Trades ───────────────────────────────────────────────────────────────

    [HttpPost("trades")]
    public async Task<IActionResult> CreateTrade(CreateTradeRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateTradeOperation>().Run(request));
    }

    [HttpPut("trades/{id:guid}")]
    public async Task<IActionResult> UpdateTrade(Guid id, UpdateTradeRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateTradeOperation>().Run(request));
    }

    [HttpDelete("trades/{id:guid}")]
    public async Task<IActionResult> DeleteTrade(Guid id)
    {
        var request = new DeleteTradeRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteTradeOperation>().Run(request));
    }

    // ── Dividends ────────────────────────────────────────────────────────────

    [HttpPost("dividends")]
    public async Task<IActionResult> CreateDividend(CreateDividendRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateDividendOperation>().Run(request));
    }

    [HttpPut("dividends/{id:guid}")]
    public async Task<IActionResult> UpdateDividend(Guid id, UpdateDividendRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateDividendOperation>().Run(request));
    }

    [HttpDelete("dividends/{id:guid}")]
    public async Task<IActionResult> DeleteDividend(Guid id)
    {
        var request = new DeleteDividendRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteDividendOperation>().Run(request));
    }

    // ── Deposits ─────────────────────────────────────────────────────────────

    [HttpPost("deposits")]
    public async Task<IActionResult> CreateDeposit(CreateDepositRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateDepositOperation>().Run(request));
    }

    [HttpPut("deposits/{id:guid}")]
    public async Task<IActionResult> UpdateDeposit(Guid id, UpdateDepositRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateDepositOperation>().Run(request));
    }

    [HttpDelete("deposits/{id:guid}")]
    public async Task<IActionResult> DeleteDeposit(Guid id)
    {
        var request = new DeleteDepositRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteDepositOperation>().Run(request));
    }

    // ── Targets ──────────────────────────────────────────────────────────────

    [HttpPost("targets")]
    public async Task<IActionResult> CreateTarget(CreateTargetRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<CreateTargetOperation>().Run(request));
    }

    [HttpPut("targets/{id:guid}")]
    public async Task<IActionResult> UpdateTarget(Guid id, UpdateTargetRequest request)
    {
        request.Id = id;
        request.SessionUserData = GetSessionModelFromJwt();

        return Ok(await _operationFactory.Get<UpdateTargetOperation>().Run(request));
    }

    [HttpDelete("targets/{id:guid}")]
    public async Task<IActionResult> DeleteTarget(Guid id)
    {
        var request = new DeleteTargetRequest { Id = id, SessionUserData = GetSessionModelFromJwt() };

        return Ok(await _operationFactory.Get<DeleteTargetOperation>().Run(request));
    }
}
