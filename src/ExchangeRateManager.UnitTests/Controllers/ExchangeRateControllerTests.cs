using ExchangeRateManager.Controllers;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.Services.Interfaces;
using Shouldly;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ExchangeRateManager.UnitTests.Controllers;

/// <summary>
/// Tests for the Exchange Rate Controller
/// </summary>
public class ExchangeRateControllerTests
{
    private readonly IExchangeRateService _exchangeRateService = Substitute.For<IExchangeRateService>();
    private readonly ExchangeRateController _controller;

    public ExchangeRateControllerTests()
    {
        _controller = new(_exchangeRateService);
    }

    [Fact]
    public async Task GetForexRate_ReturnsDto_Success()
    {
        // arrange
        ExchangeRateRequestDto? actualRequest = default;
        ExchangeRateResponseDto? actualResponse = default;

        var expectedRequest = new ExchangeRateRequestDto
        {
            FromCurrencyCode = Guid.NewGuid().ToString(),
            ToCurrencyCode = Guid.NewGuid().ToString()
        };

        var expectedResponse = new ExchangeRateResponseDto
        {
            FromCurrencyCode = expectedRequest.FromCurrencyCode,
            ToCurrencyCode = expectedRequest.ToCurrencyCode
        };

        _exchangeRateService
            .GetForexRate(Arg.Any<ExchangeRateRequestDto>())
            .Returns(x => expectedResponse)
            .AndDoes(x => actualRequest = x.Arg<ExchangeRateRequestDto>());

        // act
        var result = await _controller.GetLatestRateExchange(
            expectedRequest.FromCurrencyCode, expectedRequest.ToCurrencyCode);

        // assert
        await _exchangeRateService
            .Received(1)
            .GetForexRate(Arg.Any<ExchangeRateRequestDto>());

        result.ShouldBeOfType<OkObjectResult>();
        actualResponse = (result as OkObjectResult)?.Value as ExchangeRateResponseDto;
        expectedResponse.ShouldBeEquivalentTo(actualResponse);
        expectedRequest.ShouldBeEquivalentTo(actualRequest);
    }

    [Fact]
    public void GetForexRate_ThrowsException()
    {
        // arrange
        _exchangeRateService
            .GetForexRate(Arg.Any<ExchangeRateRequestDto>())
            .ThrowsAsync(new InvalidOperationException());

        // act
        var testAction = async () => await _controller.GetLatestRateExchange(
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // assert
        testAction.ShouldThrowAsync<InvalidOperationException>();
        _exchangeRateService
            .Received(1)
            .GetForexRate(Arg.Any<ExchangeRateRequestDto>());
    }
}