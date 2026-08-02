using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Models.Auth;

public class SessionModel
{
    public Guid Id { get; set; }
    public string Password { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
}
