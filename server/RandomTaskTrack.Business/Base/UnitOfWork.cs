using Npgsql;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Business.Base;

public class UnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;
    private bool _committedOrRolledBack;

    public NpgsqlConnection Connection => _connection;
    public NpgsqlTransaction? Transaction => _transaction;

    public UnitOfWork(NpgsqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null) return;

        _transaction = await _connection.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null) return;

        await _transaction.CommitAsync();
        _committedOrRolledBack = true;
    }

    public async Task RollbackAsync()
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            _committedOrRolledBack = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_committedOrRolledBack && _transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        catch
        {
            // swallow (we should not hide original exception from caller)
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            await _connection.DisposeAsync();
        }
    }
}
