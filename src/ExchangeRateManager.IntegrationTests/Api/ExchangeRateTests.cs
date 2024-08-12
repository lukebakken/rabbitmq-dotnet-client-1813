using DalSoft.RestClient;
using DalSoft.RestClient.Testing;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ExchangeRateManager.Tests.IntegrationTests.Api;

/// <summary>
/// Functional tests for the "ExchangeRate" endpoints
/// </summary>
public class ExchangeRateTests(TestsWebApplicationFactory factory) : IntegrationTextBase(factory), IClassFixture<TestsWebApplicationFactory>
{
    [Fact]
    public async Task GetLatestExchangeRate_Returns200()
    {
        // Act
        var response = _factory
            .CreateRestClient()
            .Resource("/ExchangeRate?fromCurrency=USD&toCurrency=EUR")
            .Get();

        // Assert
        async Task<dynamic> action() => await response
            .Act<HttpResponseMessage>(x => x.StatusCode.Should().Be(HttpStatusCode.OK))
            .Act<ExchangeRateResponseDto>(x =>
            {
                x.FromCurrencyCode.Should().BeEquivalentTo("USD");
                x.ToCurrencyCode.Should().BeEquivalentTo("EUR");
            });

        await TestHandler(action);
    }

    [Fact]
    public async Task GetLatestExchangeRate_InvalidCurrencies_Returns400()
    {
        // Act
        var response = _factory
            .CreateRestClient()
            .Resource("/ExchangeRate?fromCurrency=CRASH&toCurrency=DUMMY")
            .Get();

        
        // Assert
        async Task<dynamic> action() => await response
            .Act<HttpResponseMessage>(x => x.StatusCode.Should().Be(HttpStatusCode.BadRequest))
            .Act<ProblemDetails>(x => x.Title.Should().Be(new InvalidExchangeRateException(default!).Details.Title));

        await TestHandler(action);
    }
}