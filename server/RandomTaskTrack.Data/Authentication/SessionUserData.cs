using RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Data.Authentication;

public class SessionUserData
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
}
