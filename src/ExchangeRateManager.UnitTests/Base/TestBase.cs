using AutoFixture;

namespace ExchangeRateManager.Tests.UnitTests.Base
{
    /// <summary>
    /// The test base for common components in all unit tests
    /// </summary>
    public abstract class TestBase
    {
        protected readonly Fixture _fixture = new();
    }
}