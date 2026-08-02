using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RandomTaskTrack.Data.Authentication;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;

namespace RandomTaskTrack.API.Controllers.Base;

public class BaseController : ControllerBase
{
    protected readonly ILogger _logger;

    public BaseController(ILogger logger)
    {
        _logger = logger;
    }

    protected SessionUserData GetSessionModelFromJwt()
    {
        if (Request.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new SessionExpiredException("Invalid token");
        }

        ClaimsPrincipal user = Request.HttpContext.User!;

        string? sub = user.FindFirst(JwtTokenClaimNames.UserId)?.Value
                      ?? user.FindFirst(ClaimTypes.Sid)?.Value;

        string? email = user.FindFirst(JwtTokenClaimNames.Email)?.Value
                        ?? user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? user.FindFirst(ClaimTypes.Email)?.Value;

        string? roleClaim = user.FindFirst(JwtTokenClaimNames.Role)?.Value
                            ?? user.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(sub, out Guid userId))
        {
            _logger.LogWarning("Invalid or missing user id in JWT. Malformed token detected.");
            throw new SessionExpiredException("Invalid token");
        }

        if (string.IsNullOrEmpty(roleClaim) || !int.TryParse(roleClaim, out int roleInt))
        {
            _logger.LogWarning("Invalid or missing role claim in JWT for user id: {UserId}. Malformed token detected.", userId);
            throw new SessionExpiredException("Invalid token");
        }

        return new SessionUserData
        {
            Id = userId,
            Email = email ?? string.Empty,
            Role = (UserRole)roleInt
        };
    }
}
