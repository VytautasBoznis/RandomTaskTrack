namespace RandomTaskTrack.Data.Response.Auth.Dto;

public class SessionDto
{
    public string JwtToken { get; set; } = "";
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
