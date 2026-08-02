using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Controllers.Base;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Auth;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Request.Auth;
using RandomTaskTrack.Data.Response.Auth;

namespace RandomTaskTrack.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly OperationFactory _operationFactory;

    public AuthController(OperationFactory operationFactory, ILogger<AuthController> logger) : base(logger)
    {
        _operationFactory = operationFactory;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        LoginResponse response = await _operationFactory.Get<LoginOperation>().Run(request);

        return Ok(response);
    }

    [HttpPost("register")]
    [TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.Admin })]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        RegisterResponse response = await _operationFactory.Get<RegisterOperation>().Run(request);

        return Ok(response);
    }

    [HttpPost("change-password")]
    [TypeFilter(typeof(AuthorizationFilter), Arguments = new object[] { UserRole.User })]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        request.SessionUserData = GetSessionModelFromJwt();

        ChangePasswordResponse response = await _operationFactory.Get<ChangePasswordOperation>().Run(request);

        return Ok(response);
    }
}
