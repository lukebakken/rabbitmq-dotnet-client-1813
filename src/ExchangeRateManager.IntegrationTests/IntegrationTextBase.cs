using DalSoft.RestClient;

namespace ExchangeRateManager.IntegrationTests;

/// <summary>
/// Integration tests base components.
/// </summary>
public class IntegrationTextBase(TestsWebApplicationFactory factory)
{
    protected readonly TestsWebApplicationFactory _factory = factory;
    protected delegate Task<dynamic> TestCall();

    /// <summary>
    /// Catches any Asserts or exceptions to log the unexpected response contents before crashing.
    /// </summary>
    /// <param name="testCall">A delegate pointing to the test.</param>
    protected static async Task TestHandler(TestCall testCall)
    {
		Task<dynamic> test = Task.FromResult<dynamic>(0);
		try
		{
			test = testCall();
			await test;
        }
		catch
		{
			await test.Act<string>(x => Console.WriteLine(x));
			throw;
		}
    }
}