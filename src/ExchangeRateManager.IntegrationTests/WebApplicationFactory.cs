using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Environments = ExchangeRateManager.Common.Constants.Environments;

namespace ExchangeRateManager.IntegrationTests
{
    /// <summary>
    /// Factory for bootstrapping an application in memory for functional end to end tests.
    /// <typeparam name="TEntryPoint">A type in the entry point assembly of the application.
    /// Typically the Startup or Program classes can be used.</typeparam>
    /// <br/><br/><a href="https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0">
    /// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0
    /// </a>
    /// </summary>
    public class TestsWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder =>
            {
                builder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((builderContext, config) =>
                    {
                        config
                            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{Environments.IntegrationTests}.json"))
                            .AddEnvironmentVariables()
                            .Build();
                    })
                    .UseEnvironment(Environments.IntegrationTests)
                    .ConfigureTestServices(services =>
                    {
                        // Replace any integration tests specific services here
                        // or setup the keyed services on the appsettings.IntegrationTests.json
                    });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
