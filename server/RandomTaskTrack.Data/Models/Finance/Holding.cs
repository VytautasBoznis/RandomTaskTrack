namespace RandomTaskTrack.Data.Models.Finance;

public class Holding
{
    public Guid Id { get; set; }

    /// <summary>
    /// In the price source's own vocabulary — Yahoo wants <c>AAPL</c> for
    /// Nasdaq and <c>ASML.AS</c> for Amsterdam. Same bargain recipe families
    /// make with cuisine codes.
    /// </summary>
    public string Symbol { get; set; } = "";

    public string? Name { get; set; }
    public string Currency { get; set; } = "";

    /// <summary>Null until the first successful price refresh.</summary>
    public decimal? LastPrice { get; set; }

    public DateTime? LastPriceAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
