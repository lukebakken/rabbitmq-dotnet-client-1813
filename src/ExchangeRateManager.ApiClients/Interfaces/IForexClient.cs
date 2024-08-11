using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Common.Interfaces.Base;

namespace ExchangeRateManager.ApiClients.Interfaces;

public interface IForexClient : IClient
{
    Task<GetCurrencyExchangeRatesResponse?> GetCurrencyExchangeRates(string fromCurrency, string toCurrency);
}
