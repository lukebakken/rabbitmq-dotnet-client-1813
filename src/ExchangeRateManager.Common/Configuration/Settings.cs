using ExchangeRateManager.Common.Constants;

namespace ExchangeRateManager.Common.Configuration;

public class Settings
{
    public Dictionary<string, string?>? KeyedServices { get; set; }
    public Dictionary<string, HttpClientSettings>? HttpClients { get; set; }
    public ExchangeRateSettings ExchangeRate { get; set; } = new ExchangeRateSettings();
    public bool RunMigrationsAtStartup { get; set; }
    public string Cache { get; set; } = CacheSettings.Disabled;
}
