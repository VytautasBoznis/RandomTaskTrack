namespace RandomTaskTrack.Data.Models.Finance;

/// <summary>What a price source hands back for one symbol.</summary>
public class StockQuote
{
    public string Symbol { get; set; } = "";
    public decimal Price { get; set; }

    /// <summary>The session the price is from, so the UI can show its age.</summary>
    public DateOnly? AsOf { get; set; }
}
