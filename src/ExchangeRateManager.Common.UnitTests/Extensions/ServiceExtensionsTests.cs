using ExchangeRateManager.Common;
using ExchangeRateManager.Common.Extensions;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;

namespace ExchangeRateManager.Common.UnitTests.Extensions;

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
        IServiceCollection serviceCollection = Substitute.For<IServiceCollection>();

        List<ServiceDescriptor> actualServiceDescriptors = [];
        serviceCollection
            .When(x => x.Add(Arg.Any<ServiceDescriptor>()))
            .Do(x => actualServiceDescriptors.Add(x.Arg<ServiceDescriptor>()));

        List<ServiceDescriptor> expectedServiceDescriptors =
        [
            new ServiceDescriptor(typeof(ITestSingleComponent1), typeof(TestClass1), ServiceLifetime.Transient),
            new ServiceDescriptor(typeof(ITestSingleComponent2), typeof(TestClass2), ServiceLifetime.Singleton),
        ];

        // Act
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseSingle>();

        // Assert
        serviceCollection2.ShouldBeSameAs(serviceCollection);
        actualServiceDescriptors.ForEach(x => x.ShouldSatisfyAllConditions(
            (t) => t.IsKeyedService.ShouldBeFalse(),
            (t) => expectedServiceDescriptors.Any(e => e.ServiceType == t.ServiceType).ShouldBeTrue(),
            (t) => expectedServiceDescriptors.Any(e => e.ImplementationType == x.ImplementationType).ShouldBeTrue()));
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForKeyedService_RegistersSuccessfully()
    {
        // Arrange
        IServiceCollection serviceCollection = Substitute.For<IServiceCollection>();

        List<ServiceDescriptor> actualServiceDescriptors = [];
        serviceCollection
            .When(x => x.Add(Arg.Any<ServiceDescriptor>()))
            .Do(x => actualServiceDescriptors.Add(x.Arg<ServiceDescriptor>()));

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
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseDouble>();

        // Assert
        serviceCollection2.ShouldBeSameAs(serviceCollection);
        actualServiceDescriptors.Count(x => x.IsKeyedService).ShouldBe(2);
        actualServiceDescriptors.Count(x => !x.IsKeyedService).ShouldBe(1);

        var expectedKeyed = expectedServiceDescriptors.Where(x => x.IsKeyedService);
        actualServiceDescriptors
            .Where(x => x.IsKeyedService)
            .ShouldBeEquivalentTo(expectedKeyed);

        var expectedFactory = expectedServiceDescriptors.Single(x => !x.IsKeyedService);
        var actualFactory = actualServiceDescriptors.Single(x => !x.IsKeyedService);
        actualFactory.ImplementationFactory.ShouldNotBeNull();
        actualFactory
            .ShouldSatisfyAllConditions(
                factory => factory.Lifetime.ShouldBeEquivalentTo(expectedFactory.Lifetime),
                factory => factory.ServiceType.ShouldBeEquivalentTo(expectedFactory.ServiceType),
                factory => factory.ImplementationType.ShouldBeEquivalentTo(expectedFactory.ImplementationType));
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForOrphanService_DoesNothing()
    {
        // Arrange
        IServiceCollection serviceCollection = Substitute.For<IServiceCollection>();

        // Act
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<ITestBaseOrphan>();

        // Assert
        serviceCollection2.ShouldBeSameAs(serviceCollection);

        serviceCollection
            .DidNotReceive()
            .Add(Arg.Any<ServiceDescriptor>());
    }

    [Fact]
    public void LoadServicesFromAssemblies_NoServiceLifetimeDefined_ThrowsError()
    {
        // Arrange
        IServiceCollection serviceCollection = Substitute.For<IServiceCollection>();

        // Act
        var action = () => serviceCollection
            .LoadServicesFromAssemblies<ITestBaseNoLifetime>();

        // Assert
        action.ShouldThrow<TypeLoadException>();
        serviceCollection
            .DidNotReceive()
            .Add(Arg.Any<ServiceDescriptor>());
    }

    [Fact]
    public void LoadServicesFromAssemblies_LooksForDifferentNamespace_DoesNothing()
    {
        // Arrange
        IServiceCollection serviceCollection = Substitute.For<IServiceCollection>();

        // Act
        var serviceCollection2 = serviceCollection
            .LoadServicesFromAssemblies<IHttpClient>();

        // Assert
        serviceCollection2.ShouldBeSameAs(serviceCollection);

        serviceCollection
            .DidNotReceive()
            .Add(Arg.Any<ServiceDescriptor>());
    }

    #endregion LoadServicesFromAssemblies Tests


    #region AssertInterface Tests

    [Fact]
    public void AssertInterface_object_ThrowsError()
    {
        // Act
        var action = () => typeof(object).AssertInterface();

        //Assert
        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AssertInterface_IService_Passes()
    {
        // Act
        var action = () => typeof(IService).AssertInterface();

        //Assert
        action.ShouldNotThrow();
    }

    #endregion AssertInterface Tests
}
