using Bogus;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ExchangeRateManager.ApiClients;

[ExcludeFromCodeCoverage(Justification = "For development and integration tests purposes only.")]
public class ForexClientStub : IForexClient, ISingleton
{
    private readonly Faker<RealtimeCurrencyExchangeRateResponse> _faker = new();
    private readonly ImmutableDictionary<string, string> _currencies = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(x => new RegionInfo(x.Name))
        .DistinctBy(x => x.ISOCurrencySymbol)
        .ToImmutableDictionary(x => x.ISOCurrencySymbol, x => x.CurrencyEnglishName);

    public Task<GetCurrencyExchangeRatesResponse?> GetCurrencyExchangeRates(string fromCurrency, string toCurrency)
    {
        if (!_currencies.TryGetValue(fromCurrency = fromCurrency.ToUpper(), out var fromCurrencyName) ||
            !_currencies.TryGetValue(toCurrency = toCurrency.ToUpper(), out var toCurrencyName))
        {
            return Task.FromException<GetCurrencyExchangeRatesResponse?>(
                new BadHttpRequestException("Invalid payload. Check fields"));
        }

        _faker.RuleFor(x => x.FromCurrencyCode, fromCurrency);
        _faker.RuleFor(x => x.ToCurrencyCode, toCurrency);
        _faker.RuleFor(x => x.ExchangeRate, opt => opt.Random.Decimal(max: 1000));
        _faker.RuleFor(x => x.AskPrice, opt => opt.Random.Decimal(max: 1000));
        _faker.RuleFor(x => x.BidPrice, opt => opt.Random.Decimal(max: 1000));
        _faker.RuleFor(x => x.TimeZone, "UTC");
        _faker.RuleFor(x => x.LastRefreshed, DateTime.UtcNow.AddMinutes(-Random.Shared.Next(10)));
        _faker.RuleFor(x => x.FromCurrencyName, (opt, stub) => fromCurrencyName);
        _faker.RuleFor(x => x.ToCurrencyName, (opt, stub) => toCurrencyName);

        return Task.FromResult<GetCurrencyExchangeRatesResponse?>(new GetCurrencyExchangeRatesResponse
        {
            RealtimeCurrencyExchangeRate = _faker.Generate()
        });
    }
}
