using Npgsql;

namespace RandomTaskTrack.Interfaces.Base;

public interface IUnitOfWork : IAsyncDisposable
{
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction? Transaction { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
