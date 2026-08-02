using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Auth;

public interface IAuthRepository
{
    Task<SessionModel?> GetSessionModelByEmail(string email, IUnitOfWork unitOfWork);
}
