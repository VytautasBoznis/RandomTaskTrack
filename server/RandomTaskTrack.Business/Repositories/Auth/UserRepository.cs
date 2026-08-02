using Dapper;
using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Auth;

namespace RandomTaskTrack.Business.Repositories.Auth;

public class UserRepository : IUserRepository
{
    public async Task<bool> EmailExistsAsync(string email, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tracker.user_users WHERE lower(email) = lower(@email))",
            new { email },
            unitOfWork.Transaction);
    }

    public async Task CreateAsync(User user, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.user_users (id, email, password, role)
              VALUES (@Id, @Email, @Password, @Role)",
            new { user.Id, user.Email, user.Password, Role = (int)user.Role },
            unitOfWork.Transaction);
    }

    public async Task<User?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(
            "SELECT id, email, password, role, created_at FROM tracker.user_users WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task UpdatePasswordAsync(Guid id, string password, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "UPDATE tracker.user_users SET password = @password WHERE id = @id",
            new { id, password },
            unitOfWork.Transaction);
    }

    public async Task<int> CountAsync(IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM tracker.user_users",
            transaction: unitOfWork.Transaction);
    }
}
