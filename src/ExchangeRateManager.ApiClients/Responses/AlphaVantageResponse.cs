using System.Text.Json.Serialization;

namespace ExchangeRateManager.ApiClients.Responses
{
    public class AlphaVantageResponse
    {
        public string? Note { get; set; }
        public string? Information { get; set; }

        [JsonPropertyName("Error Message")]
        public string? ErrorMessage { get; set; }
    }
}