using AutoMapper;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.UnitTests.Base;

[ExcludeFromCodeCoverage(Justification = "Part of the test suite.")]
public abstract class MapperTestBase : TestBase
{
    protected readonly IMapper _mockedMapper;
    protected readonly IMapper _actualMapper;

    protected MapperTestBase(MapperConfiguration mapperConfiguration)
    {
        _actualMapper = new Mapper(mapperConfiguration);
        _mockedMapper = Substitute.For<IMapper>();
    }

    /// <summary>
    /// Setups the mocked method to consume the actual automapper.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    protected void SetupMap<TSource, TDest>()
        => _mockedMapper
            .Map<TDest>(Arg.Any<TSource>())
            .Returns(source => _actualMapper.Map<TDest>(source.Arg<TSource>()));

    /// <summary>
    /// Setups the mocked method for a manual mapping between two types.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    /// <param name="returns">A function to check the source, returning an object.</param>
    protected void SetupMap<TSource, TDest>(TSource source, Func<TSource, TDest> returns)
        => _mockedMapper
            .Map<TDest>(source)
            .Returns(x => returns(x.Arg<TSource>()));

    /// <summary>
    /// Setups the mocked method to consume the actual automapper and allowing to check the source object.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="callback">A callback to check the source type before converting.</param>
    protected void SetupMap<TSource, TDest>(Action<TSource> callback)
        => _mockedMapper
            .Map<TDest>(Arg.Any<TSource>())
            .Returns(source =>
            {
                callback(source.Arg<TSource>());
                return _actualMapper.Map<TDest>(source.Arg<TSource>());
            });

    /// <summary>
    /// Verifies the map between two types has been called for set number of times, with an expected source.
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void ReceivedMap<TSource, TDest>(TSource source, int times)
        => _mockedMapper
            .Received(times)
            .Map<TDest>(source);

    /// <summary>
    /// Verifies the map between two types has been called for set number of times, regardless of the source.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void ReceivedMap<TSource, TDest>(int times)
        => _mockedMapper
            .Received(times)
            .Map<TDest>(Arg.Any<TSource>());

    /// <summary>
    /// Verifies the map between two types has NOT been called, with an expected source.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    protected void DidNotReceiveMap<TSource, TDest>(TSource source)
        => _mockedMapper
            .DidNotReceive()
            .Map<TDest>(source);

    /// <summary>
    /// Verifies the map between two types has NOT been called.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void DidNotReceiveMap<TSource, TDest>()
        => _mockedMapper
            .DidNotReceive()
            .Map<TDest>(Arg.Any<TSource>());
}
