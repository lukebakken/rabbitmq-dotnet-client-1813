using ExchangeRateManager.Controllers;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ExchangeRateManager.Tests.UnitTests.Controllers;

/// <summary>
/// Tests for the Exchange Rate Controller
/// </summary>
public class ExchangeRateControllerTests
{
    private readonly Mock<IExchangeRateService> _exchangeRateServiceMock = new();
    private readonly ExchangeRateController _controller;

    public ExchangeRateControllerTests()
    {
        _controller = new(_exchangeRateServiceMock.Object);
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

        _exchangeRateServiceMock
            .Setup(x => x.GetForexRate(It.IsAny<ExchangeRateRequestDto>()))
            .Callback<ExchangeRateRequestDto>(x => actualRequest = x)
            .ReturnsAsync(expectedResponse);

        // act
        var result = await _controller.GetLatestRateExchange(
            expectedRequest.FromCurrencyCode, expectedRequest.ToCurrencyCode);

        // assert
        _exchangeRateServiceMock
            .Verify(x => x.GetForexRate(It.IsAny<ExchangeRateRequestDto>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
        actualResponse = (result as OkObjectResult)?.Value as ExchangeRateResponseDto;
        expectedResponse.Should().BeEquivalentTo(actualResponse);
        expectedRequest.Should().BeEquivalentTo(actualRequest);
    }

    [Fact]
    public void GetForexRate_ThrowsException()
    {
        // arrange
        _exchangeRateServiceMock
            .Setup(x => x.GetForexRate(It.IsAny<ExchangeRateRequestDto>()))
            .ThrowsAsync(new TestException());

        // act
        var testAction = async () => await _controller.GetLatestRateExchange(
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // assert
        testAction.Should().ThrowAsync<TestException>();
        _exchangeRateServiceMock
            .Verify(x => x.GetForexRate(It.IsAny<ExchangeRateRequestDto>()), Times.Once);
    }
}