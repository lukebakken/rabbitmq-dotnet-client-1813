using System.Text.Json.Serialization;

namespace ExchangeRateManager.ApiClients.Responses
{
    public class GetCurrencyExchangeRatesResponse : AlphaVantageResponse
    {
        [JsonPropertyName("Realtime Currency Exchange Rate")]
        public RealtimeCurrencyExchangeRateResponse? RealtimeCurrencyExchangeRate { get; set; }
    }
}