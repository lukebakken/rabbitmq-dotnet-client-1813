using ExchangeRateManager.Common.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Moq;

using Environments = ExchangeRateManager.Common.Constants.Environments;

namespace ExchangeRateManager.Tests.UnitTests.Common.Extensions;

/// <summary>
/// Environment Identification Extentions Tests
/// </summary>
public class EnvironmentExtensionsTests
{
    [Theory]
    [InlineData(0, nameof(Environments.Local))]
    [InlineData(1, nameof(Environments.IntegrationTests))]
    [InlineData(2, nameof(Environments.Development))]
    [InlineData(3, nameof(Environments.Test))]
    [InlineData(4, nameof(Environments.Staging))]
    [InlineData(5, nameof(Environments.Production))]
    public void TestEnvironmentChecks(int index, string environment)
    {
        //Arrange
        Mock<IWebHostEnvironment> _webHostEnvironmentMock = new();

        _webHostEnvironmentMock
            .Setup(x => x.EnvironmentName)
            .Returns(environment);

        var expected = Enumerable
            .Range(0, 7)
            .Select(x => x == index || x == 6 && index > 3)
            .ToList();


        // Act
        var webHostEnvironment = _webHostEnvironmentMock.Object;
        var results = new List<bool> {
            _webHostEnvironmentMock.Object.IsLocal(),
            _webHostEnvironmentMock.Object.IsIntegrationTests(),
            _webHostEnvironmentMock.Object.IsDevelopment(),
            _webHostEnvironmentMock.Object.IsTest(),
            _webHostEnvironmentMock.Object.IsStaging(),
            _webHostEnvironmentMock.Object.IsProduction(),
            _webHostEnvironmentMock.Object.IsHighLevel(),
        };

        // Assert
        results.Should().BeEquivalentTo(expected);
    }
}
