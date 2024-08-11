using DalSoft.RestClient;
using DalSoft.RestClient.Testing;
using ExchangeRateManager.Dtos;
using FluentAssertions;
using System.Net;

namespace ExchangeRateManager.Tests.IntegrationTests.Api;

/// <summary>
/// Functional tests for the "ExchangeRate" endpoints
/// </summary>
public class ExchangeRateTests(TestsWebApplicationFactory factory) : IClassFixture<TestsWebApplicationFactory>
{
    private readonly TestsWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GetLatestExchangeRate_Returns200()
    {
        await _factory
            .CreateRestClient()
            .Resource("/ExchangeRate?fromCurrency=USD&toCurrency=EUR")
            .Get()
            .Act<HttpResponseMessage>(x => x.StatusCode.Should().Be(HttpStatusCode.OK))
            .Act<ExchangeRateResponseDto>(x =>
            {
                x.FromCurrencyCode.Should().BeEquivalentTo("USD");
                x.ToCurrencyCode.Should().BeEquivalentTo("EUR");
            });
    }

    [Fact]
    public async Task GetLatestExchangeRate_InvalidCurrencies_Returns400()
    {
        await _factory
            .CreateRestClient()
            .Resource("/ExchangeRate?fromCurrency=CRASH&toCurrency=DUMMY")
            .Get()
            .Act<HttpResponseMessage>(x => x.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }
}