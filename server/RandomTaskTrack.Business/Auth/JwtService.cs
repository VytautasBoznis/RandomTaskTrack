using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Business.Auth;

public class JwtService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }

    /// <summary>
    /// 30 days by default. The tablet is a permanently-signed-in kiosk with no
    /// keyboard — a short-lived token would mean re-typing a password on a
    /// touchscreen every hour.
    /// </summary>
    public int ExpiryInMinutes =>
        _options.ExpiryInMinutes > 0 ? _options.ExpiryInMinutes : AuthConstants.DefaultExpiryInMinutes;

    private TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _key,
        ValidateIssuer = !string.IsNullOrEmpty(_options.Issuer),
        ValidIssuer = _options.Issuer,
        ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
        ValidAudience = _options.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    public string GenerateToken(Guid userId, string email, UserRole role, out DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtTokenClaimNames.UserId, userId.ToString()),
            new Claim(JwtTokenClaimNames.Email, email),
            new Claim(JwtTokenClaimNames.Role, ((int)role).ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        expiresAt = DateTime.UtcNow.AddMinutes(ExpiryInMinutes);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrEmpty(_options.Issuer) ? null : _options.Issuer,
            audience: string.IsNullOrEmpty(_options.Audience) ? null : _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters, out _);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning("JwtService.ValidateToken failed: {ExceptionType} — {Message}", ex.GetType().Name, ex.Message);
            return null;
        }
    }
}
