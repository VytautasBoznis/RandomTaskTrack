using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Domains;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Domains;
using RandomTaskTrack.Data.Response.Domains;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
public class DomainsController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public DomainsController(OperationFactory operationFactory, ILogger<DomainsController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetDomainsRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        GetDomainsResponse response = await _operationFactory.Get<GetDomainsOperation>().Run(request);

        return Ok(response);
    }
}
