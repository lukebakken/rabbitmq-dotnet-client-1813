using ExchangeRateManager.ApiClients;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.Common;
using ExchangeRateManager.Common.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace ExchangeRateManager.Tests.UnitTests.Common;

/// <summary>
/// Tests for the KeyedServiceFactory
/// </summary>
public class KeyedServiceFactoryTests
{
    private readonly Mock<IKeyedServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IOptionsMonitor<Settings>> _optionsMonitorMock = new();

    [Fact]
    public void GetRequiredKeyedService_ServiceIsMapped_ReturnsService()
    {
        // Arrange
        Settings settings = new()
        {
            KeyedServices = new Dictionary<string, string?>
            {
                { typeof(IForexClient).FullName!, typeof(ForexClientStub).FullName! }
            }
        };
        _optionsMonitorMock
            .Setup(x => x.CurrentValue)
            .Returns(settings);

        _serviceProviderMock
            .Setup(x => x.GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName))
            .Returns(new ForexClientStub());

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProviderMock.Object, _optionsMonitorMock.Object);
        var service = keyedServiceFactory.Create<IForexClient>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<ForexClientStub>();
        _optionsMonitorMock.Verify(x => x.CurrentValue, Times.Once);
        _serviceProviderMock
            .Verify(x => x.GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName), Times.Once);

    }

    [Fact]
    public void GetRequiredKeyedService_ServiceIsMissing_ThrowsError()
    {
        // Arrange
        Settings settings = new()
        {
            KeyedServices = new Dictionary<string, string?>
            {
                { typeof(IForexClient).FullName!, null }
            }
        };
        _optionsMonitorMock
            .Setup(x => x.CurrentValue)
            .Returns(settings);

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProviderMock.Object, _optionsMonitorMock.Object);
        var action = keyedServiceFactory.Create<IForexClient>;

        // Assert
        action.Should().Throw<TypeLoadException>();

        _optionsMonitorMock.Verify(x => x.CurrentValue, Times.Once);
        _serviceProviderMock
            .Verify(x => x.GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName), Times.Never);
    }

    [Fact]
    public void GetRequiredKeyedService_MappingIsMissing_ThrowsError()
    {
        // Arrange
        Settings settings = new() { };
        _optionsMonitorMock
            .Setup(x => x.CurrentValue)
            .Returns(settings);

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProviderMock.Object, _optionsMonitorMock.Object);
        var action = keyedServiceFactory.Create<IForexClient>;

        // Assert
        action.Should().Throw<TypeLoadException>();
        _optionsMonitorMock.Verify(x => x.CurrentValue, Times.Once);
        _serviceProviderMock
            .Verify(x => x.GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName), Times.Never);
    }
}
