namespace RandomTaskTrack.Data.Models.Finance;

public class Currency
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>
    /// Units of this currency per one unit of the base. USD at 1.08 means
    /// 1 EUR = 1.08 USD, so converting an amount in USD to EUR divides by this.
    /// The base currency is the row with 1.
    /// </summary>
    public decimal RateToBase { get; set; }

    public DateTime UpdatedAt { get; set; }
}
