using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using RandomTaskTrack.Business.Auth;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;

namespace RandomTaskTrack.API.ActionFilters;

public class AuthorizationFilter : IAsyncActionFilter
{
    public UserRole MinimumRole { get; set; }

    public AuthorizationFilter(UserRole minimumRole = UserRole.User)
    {
        MinimumRole = minimumRole;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<AuthorizationFilter>>();

        string? authorizationHeader = context.HttpContext.Request.Headers[AuthConstants.BearerAuthorizationHeaderName];

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith(AuthConstants.BearerPrefix + " "))
        {
            logger.LogWarning("AuthorizationFilter: missing or malformed Authorization header.");
            throw new UnauthorizedException("Unauthorized", "User unauthorized", System.Net.HttpStatusCode.Unauthorized, ExceptionCodes.USER_UNAUTHORIZED);
        }

        string authToken = authorizationHeader[(AuthConstants.BearerPrefix.Length + 1)..];

        var jwtService = context.HttpContext.RequestServices.GetRequiredService<JwtService>();
        var principal = jwtService.ValidateToken(authToken);

        if (principal == null)
        {
            logger.LogWarning("AuthorizationFilter: token validation failed.");
            throw new UnauthorizedException("Unauthorized", "User unauthorized", System.Net.HttpStatusCode.Unauthorized, ExceptionCodes.USER_UNAUTHORIZED);
        }

        var roleClaim = principal.FindFirstValue(JwtTokenClaimNames.Role);
        if (!int.TryParse(roleClaim, out var roleValue) || (UserRole)roleValue < MinimumRole)
        {
            throw new UnauthorizedException("Unauthorized", "User unauthorized", System.Net.HttpStatusCode.Unauthorized, ExceptionCodes.USER_UNAUTHORIZED);
        }

        await next();
    }
}
