using ExchangeRateManager.ApiClients;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.Common;
using ExchangeRateManager.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace ExchangeRateManager.Common.UnitTests;

/// <summary>
/// Tests for the KeyedServiceFactory
/// </summary>
public class KeyedServiceFactoryTests
{
    private readonly IKeyedServiceProvider _serviceProvider = Substitute.For<IKeyedServiceProvider>();
    private readonly IOptionsMonitor<Settings> _optionsMonitor = Substitute.For<IOptionsMonitor<Settings>>();

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
        _optionsMonitor
            .CurrentValue
            .Returns(settings);

        _serviceProvider
            .GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName)
            .Returns(new ForexClientStub());

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProvider, _optionsMonitor);
        var service = keyedServiceFactory.Create<IForexClient>();

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ForexClientStub>();
        _ = _optionsMonitor.Received(1).CurrentValue;

        _serviceProvider
            .GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName);

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
        _optionsMonitor
            .CurrentValue
            .Returns(settings);

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProvider, _optionsMonitor);
        var action = keyedServiceFactory.Create<IForexClient>;

        // Assert
        action.ShouldThrow<TypeLoadException>();
        _ = _optionsMonitor.Received(1).CurrentValue;

        _serviceProvider
            .DidNotReceive()
            .GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName);
    }

    [Fact]
    public void GetRequiredKeyedService_MappingIsMissing_ThrowsError()
    {
        // Arrange
        Settings settings = new() { };
        _optionsMonitor
            .CurrentValue
            .Returns(settings);

        // Act
        var keyedServiceFactory = new KeyedServiceFactory(_serviceProvider, _optionsMonitor);
        var action = keyedServiceFactory.Create<IForexClient>;

        // Assert
        action.ShouldThrow<TypeLoadException>();
        _ = _optionsMonitor.Received(1).CurrentValue;

        _serviceProvider
            .DidNotReceive()
            .GetRequiredKeyedService(typeof(IForexClient), typeof(ForexClientStub).FullName);
    }
}
