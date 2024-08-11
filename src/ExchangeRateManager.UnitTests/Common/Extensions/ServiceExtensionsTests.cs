using ExchangeRateManager.Common;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExchangeRateManager.Tests.UnitTests.Common.Extensions;

/// <summary>
/// Tests for some of the service extensions.
/// </summary>
public class ServiceExtensionsTests
{
    private interface ITestBaseSingle { }
    private interface ITestSingleComponent1 : ITestBaseSingle { }
    private interface ITestSingleComponent2 : ITestBaseSingle { }
    private class TestClass1 : ITestSingleComponent1, ITransient { }
    private class TestClass2 : ITestSingleComponent2, ISingleton { }

    private interface ITestBaseDouble { }
    private interface ITestDoubleComponent : ITestBaseDouble { }
    private class TestClassDoubled1 : ITestDoubleComponent, IScoped { }
    private class TestClassDoubled2 : ITestDoubleComponent, ITransient { }

    private interface ITestBaseOrphan { }

    private interface ITestBaseNoLifetime { }
    private interface ITestSingleComponent3 : ITestBaseNoLifetime { }
    private class TestClass3 : ITestSingleComponent3 { }

    #region LoadServicesFromAssemblies Tests

    [Fact]
    public void LoadServicesFromAssemblies_LooksForSingleService_RegistersSuccessfully()
    {
        // Arrange
        Mock<IServiceCollection> serviceCollectionMock = new();

        List<ServiceDescriptor> actualServiceDescriptors  = [];
        serviceCollectionMock
            .Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback(actualServiceDescriptors.Add);

        List<ServiceDescriptor> expectedServiceDescriptors =
        [
            new ServiceDescriptor(typeof(ITestSingleComponent1), typeof(TestClass1), ServiceLifetime.Transient),
            new ServiceDescriptor(typeof(ITestSingleComponent2), typeof(TestClass2), ServiceLifetime.Singleton),
        ];

        // Act
        var serviceCollection = serviceCollectionMock.Object;
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseSingle>();

        // Assert
        serviceCollection2.Should().BeSameAs(serviceCollection);
        actualServiceDescriptors.Should().BeEquivalentTo(expectedServiceDescriptors, options => options
            .Including(x => x.ServiceType)
            .Including(x => x.ImplementationType)
            .Including(x => x.Lifetime));

        actualServiceDescriptors.All(x => !x.IsKeyedService).Should().BeTrue();
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForKeyedService_RegistersSuccessfully()
    {
        // Arrange
        Mock<IServiceCollection> serviceCollectionMock = new();

        List<ServiceDescriptor> actualServiceDescriptors = [];
        serviceCollectionMock
            .Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback(actualServiceDescriptors.Add);

        List<ServiceDescriptor> expectedServiceDescriptors =
        [
            new ServiceDescriptor(
                typeof(ITestDoubleComponent),
                typeof(TestClassDoubled1).FullName,
                typeof(TestClassDoubled1), ServiceLifetime.Scoped),

            new ServiceDescriptor(
                typeof(ITestDoubleComponent),
                typeof(TestClassDoubled2).FullName,
                typeof(TestClassDoubled2), ServiceLifetime.Transient),
    
            new ServiceDescriptor(
                typeof(ITestDoubleComponent),
                sp => sp.GetRequiredService<KeyedServiceFactory>().Create(typeof(ITestDoubleComponent)),
                ServiceLifetime.Transient)
        ];

        // Act
        var serviceCollection = serviceCollectionMock.Object;
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseDouble>();

        // Assert
        serviceCollection2.Should().BeSameAs(serviceCollection);
        actualServiceDescriptors.Count(x => x.IsKeyedService).Should().Be(2);
        actualServiceDescriptors.Count(x => !x.IsKeyedService).Should().Be(1);

        var expectedKeyed = expectedServiceDescriptors.Where(x => x.IsKeyedService);
        actualServiceDescriptors
            .Where(x => x.IsKeyedService).Should()
            .BeEquivalentTo(expectedKeyed, options => options
            .Including(x => x.ServiceType)
            .Including(x => x.KeyedImplementationType)
            .Including(x => x.ServiceKey)
            .Including(x => x.Lifetime));

        var expectedFactory = expectedServiceDescriptors.Single(x => !x.IsKeyedService);
        var actualFactory = actualServiceDescriptors.Single(x => !x.IsKeyedService);
        actualFactory.ImplementationFactory.Should().NotBeNull();
        actualFactory.Should()
            .BeEquivalentTo(expectedFactory, options => options
            .Including(x => x.ServiceType)
            .Including(x => x.Lifetime));
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForOrphanService_DoesNothing()
    {
        // Arrange
        Mock<IServiceCollection> serviceCollectionMock = new();

        List<ServiceDescriptor> actualServiceDescriptors = [];
        serviceCollectionMock
            .Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback(actualServiceDescriptors.Add);

        // Act
        var serviceCollection = serviceCollectionMock.Object;
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseOrphan>();

        // Assert
        serviceCollection2.Should().BeSameAs(serviceCollection);
        actualServiceDescriptors.Should().BeEmpty();
    }

    [Fact]
    public void LoadServicesFromAssemblies_NoServiceLifetimeDefined_ThrowsError()
    {
        // Arrange
        Mock<IServiceCollection> serviceCollectionMock = new();

        // Act
        var serviceCollection = serviceCollectionMock.Object;

        var action = () => serviceCollection
            .LoadServicesFromAssemblies<ITestBaseNoLifetime>();

        // Assert
        action.Should().Throw<TypeLoadException>();
        serviceCollectionMock
            .Verify(x => x.Add(It.IsAny<ServiceDescriptor>()), Times.Never);
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForDifferentNamespace_DoesNothing()
    {
        // Arrange
        Mock<IServiceCollection> serviceCollectionMock = new();

        List<ServiceDescriptor> actualServiceDescriptors = [];
        serviceCollectionMock
            .Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback(actualServiceDescriptors.Add);

        // Act
        var serviceCollection = serviceCollectionMock.Object;
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ISetup>();

        // Assert
        serviceCollection2.Should().BeSameAs(serviceCollection);
        actualServiceDescriptors.Should().BeEmpty();
    }

    #endregion LoadServicesFromAssemblies Tests


    #region AssertInterface Tests

    [Fact]
    public void AssertInterface_object_ThrowsError()
    {
        // Act
        var action = () => typeof(object).AssertInterface();

        //Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssertInterface_IService_Passes()
    {
        // Act
        var action = () => typeof(IService).AssertInterface();

        //Assert
        action.Should().NotThrow();
    }

    #endregion AssertInterface Tests


}
