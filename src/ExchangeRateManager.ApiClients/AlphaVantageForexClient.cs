using ExchangeRateManager.ApiClients.Base;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExchangeRateManager.ApiClients;

[ExcludeFromCodeCoverage(Justification = "Untestable third party API Client")]
public class AlphaVantageForexClient(IHttpClientFactory httpClientFactory, ILogger<AlphaVantageForexClient> logger)
    : HttpClientBase("Alpha Vantage API", httpClientFactory.CreateClient<AlphaVantageForexClient>(), logger),
        IForexClient, IScoped, IHttpClient
{
    public async Task<GetCurrencyExchangeRatesResponse?> GetCurrencyExchangeRates(string fromCurrency, string toCurrency)
    {
        var query = _httpClient.BaseAddress!.Query;
        query = QueryHelpers.AddQueryString(query, "function", "CURRENCY_EXCHANGE_RATE");
        query = QueryHelpers.AddQueryString(query, "from_currency", fromCurrency);
        query = QueryHelpers.AddQueryString(query, "to_currency", toCurrency);

        var response = await HandleResponse<GetCurrencyExchangeRatesResponse>(
            async () => await _httpClient.GetAsync(query));

        AssertErrorMessage(response);
        AssertCallLimitReach(response);

        return response;
    }

    private void AssertErrorMessage(GetCurrencyExchangeRatesResponse? response, [CallerMemberName] string callerMethod = default!)
    {
        if (!string.IsNullOrWhiteSpace(response?.ErrorMessage) &&
            response.ErrorMessage.Contains("Invalid API call."))
        {
            _logger.LogError("[{callerClass}] '{callerMethod}' {errorMessage}", nameof(AlphaVantageForexClient), callerMethod, response.ErrorMessage);
            throw new BadHttpRequestException("Invalid payload. Check fields");
        }
    }

    private void AssertCallLimitReach(GetCurrencyExchangeRatesResponse? response, [CallerMemberName] string callerMethod = default!)
    {
        if (!string.IsNullOrWhiteSpace(response?.Note) && response.Note.Contains("premium") ||
            !string.IsNullOrWhiteSpace(response?.Information) && response.Information.Contains("premium"))
        {
            _logger.LogWarning("[{callerClass}] '{callerMethod}' call limit reached.", nameof(AlphaVantageForexClient), callerMethod);
            throw new HttpClientLimitReachedException(ClientName);
        }
    }
}
