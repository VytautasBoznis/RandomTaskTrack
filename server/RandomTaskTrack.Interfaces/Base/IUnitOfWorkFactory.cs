namespace RandomTaskTrack.Interfaces.Base;

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> CreateAsync();
}
