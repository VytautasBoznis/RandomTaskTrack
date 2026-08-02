using Microsoft.Extensions.DependencyInjection;

namespace RandomTaskTrack.Business.Base;

public class OperationFactory
{
    private readonly IServiceProvider _provider;

    public OperationFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Get<T>() where T : class
    {
        return _provider.GetRequiredService<T>();
    }
}
