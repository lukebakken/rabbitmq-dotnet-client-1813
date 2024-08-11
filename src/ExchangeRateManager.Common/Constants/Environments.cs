using BuiltinEnvironments = Microsoft.Extensions.Hosting.Environments;

namespace ExchangeRateManager.Common.Constants;

public static class Environments
{
    public static readonly string Local = "Local";
    public static readonly string Development = BuiltinEnvironments.Development;
    public static readonly string Test = "Test";
    public static readonly string Staging = BuiltinEnvironments.Staging;
    public static readonly string Production = BuiltinEnvironments.Production;
    public static readonly string IntegrationTests = "IntegrationTests";
}
