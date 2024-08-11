using Hangfire.Dashboard;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Hanfire;

[ExcludeFromCodeCoverage(Justification = "Untestable code")]
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Manages Hangfire access authorization
    /// Returns true by here for demonstration purposes.
    /// </summary>
    public bool Authorize(DashboardContext context) => true;
}