using AutoMapper;
using Moq;
using Moq.Language.Flow;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Tests.UnitTests.Base;

[ExcludeFromCodeCoverage(Justification = "Part of the test suite.")]
public abstract class MapperTestBase : TestBase
{
    protected readonly Mock<IMapper> _mapperMock;
    protected readonly IMapper _mockedMapper;
    protected readonly IMapper _actualMapper;

    protected MapperTestBase(MapperConfiguration mapperConfiguration)
    {
        _mapperMock = new Mock<IMapper>();
        _actualMapper = new Mapper(mapperConfiguration);
        _mockedMapper = _mapperMock.Object;
    }

    /// <summary>
    /// Setups the mocked method for a manual mapping between two types.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    /// <param name="dest">The object to return</param>
    /// <returns></returns>
    protected IReturnsResult<IMapper> SetupMap<TSource, TDest>(TSource source, TDest dest)
        => _mapperMock
            .Setup(x => x.Map<TDest>(source))
            .Returns(dest);

    /// <summary>
    /// Setups the mocked method for a manual mapping between two types.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    /// <param name="returns">A function to check the source, returning an object.</param>
    protected void SetupMap<TSource, TDest>(TSource source, Func<TSource, TDest> returns)
        => _mapperMock
            .Setup(x => x.Map<TDest>(source))
            .Returns(returns);

    /// <summary>
    /// Setups the mocked method to consume the actual automapper.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    protected void SetupMap<TSource, TDest>()
        => _mapperMock
            .Setup(x => x.Map<TDest>(It.IsAny<TSource>()))
            .Returns((TSource source) => _actualMapper.Map<TDest>(source));

    /// <summary>
    /// Setups the mocked method to consume the actual automapper and allowing to check the source object.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="callback">A callback to check the source type before converting.</param>
    protected void SetupMap<TSource, TDest>(Action<TSource> callback)
        => _mapperMock
            .Setup(x => x.Map<TDest>(It.IsAny<TSource>()))
            .Returns((TSource source) =>
            {
                callback(source);
                _actualMapper.Map<TDest>(source);
            });

    /// <summary>
    /// Verifies the mocked method for a manual mapping between two types.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void VerifyMap<TSource, TDest>(TSource source, Func<Times> times)
        => _mapperMock.Verify(x => x.Map<TDest>(source), times);

    /// <summary>
    /// Verifies the mocked method for a manual mapping between two types.
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="source">The expected source object. <see cref="It"/> members are allowed</param>
    protected void VerifyMap<TSource, TDest>(TSource source, Times times)
        => _mapperMock.Verify(x => x.Map<TDest>(source), times);

    /// <summary>
    /// Verifies the mocked method consumed by the automapper
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void VerifyMap<TSource, TDest>(Times times)
        => _mapperMock.Verify(x => x.Map<TDest>(It.IsAny<TSource>()), times);

    /// <summary>
    /// Verifies the mocked method consumed by the automapper
    /// </summary>
    /// <typeparam name="TSource">The Source type</typeparam>
    /// <typeparam name="TDest">The Destination type</typeparam>
    /// <param name="times">The number of times a method is expected to be method.</param>
    protected void VerifyMap<TSource, TDest>(Func<Times> times)
        => _mapperMock.Verify(x => x.Map<TDest>(It.IsAny<TSource>()), times);
}
