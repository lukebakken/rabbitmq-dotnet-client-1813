namespace ExchangeRateManager.Common.Configuration;

public class HttpClientSettings
{
    public string? BaseAddress { get; set; }
    public string? Authorization { get; set; }
    public string? QueryParams { get; set; }
}