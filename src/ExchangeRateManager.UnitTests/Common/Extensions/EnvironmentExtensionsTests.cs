using ExchangeRateManager.Common.Extensions;
using Shouldly;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NSubstitute;

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
        IWebHostEnvironment _webHostEnvironment = Substitute.For<IWebHostEnvironment>();

        _webHostEnvironment
            .EnvironmentName
            .Returns(environment);

        var expected = Enumerable
            .Range(0, 7)
            .Select(x => x == index || x == 6 && index > 3)
            .ToList();


        // Act
        var results = new List<bool> {
            _webHostEnvironment.IsLocal(),
            _webHostEnvironment.IsIntegrationTests(),
            _webHostEnvironment.IsDevelopment(),
            _webHostEnvironment.IsTest(),
            _webHostEnvironment.IsStaging(),
            _webHostEnvironment.IsProduction(),
            _webHostEnvironment.IsHighLevel()
        };

        // Assert
        results.ShouldBeEquivalentTo(expected);
    }
}
