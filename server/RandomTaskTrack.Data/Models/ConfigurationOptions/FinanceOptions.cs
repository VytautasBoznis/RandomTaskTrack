using RandomTaskTrack.Data.Models.Constants;

namespace RandomTaskTrack.Data.Models.ConfigurationOptions;

public class FinanceOptions
{
    /// <summary>
    /// The currency everything is reported in. Must match a row in
    /// tracker.fin_currencies, and that row must have rate_to_base = 1.
    /// </summary>
    public string BaseCurrency { get; set; } = "EUR";

    public string Provider { get; set; } = PriceSourceNames.Yahoo;

    /// <summary>
    /// Yahoo needs no key and no account, which is the whole reason it is the
    /// default: the tab works the moment the app is deployed.
    /// </summary>
    public string BaseUrl { get; set; } = "https://query1.finance.yahoo.com";
}
