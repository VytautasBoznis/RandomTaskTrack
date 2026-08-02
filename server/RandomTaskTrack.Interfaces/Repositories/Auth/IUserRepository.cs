using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Auth;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, IUnitOfWork unitOfWork);
    Task CreateAsync(User user, IUnitOfWork unitOfWork);
    Task<User?> GetByIdAsync(Guid id, IUnitOfWork unitOfWork);
    Task UpdatePasswordAsync(Guid id, string password, IUnitOfWork unitOfWork);
    Task<int> CountAsync(IUnitOfWork unitOfWork);
}
