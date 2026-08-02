using Dapper;
using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Auth;

namespace RandomTaskTrack.Business.Repositories.Auth;

public class AuthRepository : IAuthRepository
{
    public async Task<SessionModel?> GetSessionModelByEmail(string email, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<SessionModel>(
            "SELECT id, password, email, role FROM tracker.user_users WHERE lower(email) = lower(@email)",
            new { email },
            unitOfWork.Transaction);
    }
}
