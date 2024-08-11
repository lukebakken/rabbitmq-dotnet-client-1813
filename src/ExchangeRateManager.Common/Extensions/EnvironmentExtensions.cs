using ExchangeRateManager.Common.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Environments = ExchangeRateManager.Common.Constants.Environments;

namespace ExchangeRateManager.Common.Extensions;

/// <summary>
/// Environment identification extensions.
/// </summary>
public static class EnvironmentExtensions
{
    public static bool IsLocal(this IWebHostEnvironment webHostEnvironment) => webHostEnvironment.IsEnvironment(Environments.Local);
    public static bool IsTest(this IWebHostEnvironment webHostEnvironment) => webHostEnvironment.IsEnvironment(Environments.Test);
    public static bool IsIntegrationTests(this IWebHostEnvironment webHostEnvironment) => webHostEnvironment.IsEnvironment(Environments.IntegrationTests);
    public static bool IsHighLevel(this IWebHostEnvironment webHostEnvironment) => webHostEnvironment.IsStaging() || webHostEnvironment.IsProduction();
}
