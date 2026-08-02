using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Services;

/// <summary>
/// Keeps the materialization horizon topped up. Runs once at startup and then
/// on an interval — the dashboard reads plain rows, so if this stops, upcoming
/// tasks quietly stop appearing rather than erroring, which is why failures are
/// logged loudly.
/// </summary>
public class RecurrenceMaterializerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SchedulerOptions _options;
    private readonly ILogger<RecurrenceMaterializerHostedService> _logger;

    public RecurrenceMaterializerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> options,
        ILogger<RecurrenceMaterializerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, _options.MaterializeIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a bad run kill the loop — a transient DB blip
                // should not stop task generation until the next restart.
                _logger.LogError(ex, "Recurrence materialization failed; retrying in {Interval}", interval);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
        var materializer = scope.ServiceProvider.GetRequiredService<IRecurrenceMaterializer>();

        await using IUnitOfWork unitOfWork = await factory.CreateAsync();
        await unitOfWork.BeginTransactionAsync();

        try
        {
            await materializer.MaterializeAllAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
