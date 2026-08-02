using Microsoft.Extensions.Options;
using Npgsql;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Business.Base;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly DatabaseOptions databaseOptions;

    public UnitOfWorkFactory(IOptions<DatabaseOptions> databaseOption)
    {
        databaseOptions = databaseOption?.Value ?? throw new ArgumentNullException(nameof(databaseOption));
    }

    public async Task<IUnitOfWork> CreateAsync()
    {
        var conn = new NpgsqlConnection(databaseOptions.ConnectionString);
        await conn.OpenAsync();

        return new UnitOfWork(conn);
    }
}
