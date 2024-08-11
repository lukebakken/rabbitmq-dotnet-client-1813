namespace ExchangeRateManager.Common.Configuration;

public class ExchangeRateSettings
{
    public bool PreferLiveData { get; set; } = false;
    public int ExpirationMinutes { get; set; } = 5;
}