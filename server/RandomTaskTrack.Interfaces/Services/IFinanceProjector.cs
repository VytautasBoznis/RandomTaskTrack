using RandomTaskTrack.Data.Dtos.Finance;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Services;

/// <summary>
/// Derives the money curve on read. This is the deliberate reverse of
/// <see cref="IRecurrenceMaterializer"/>: task instances are written ahead of
/// time because each one has to be individually editable over a 21-day horizon,
/// while a 30-year projection is hundreds of buckets nobody edits that change
/// wholesale the moment one flow does.
/// </summary>
public interface IFinanceProjector
{
    /// <summary>The overview numbers: cash, deposits, holdings and net worth as of today.</summary>
    Task<FinanceOverviewDto> BuildOverviewAsync(IUnitOfWork unitOfWork);

    /// <param name="historyMonths">Months of ledger actuals to include behind today.</param>
    /// <param name="months">Months to project forward.</param>
    /// <param name="stockGrowthPct">Assumed annual return on holdings, as a percentage.</param>
    Task<List<ProjectionPointDto>> ProjectAsync(
        int historyMonths,
        int months,
        decimal stockGrowthPct,
        IUnitOfWork unitOfWork);
}
