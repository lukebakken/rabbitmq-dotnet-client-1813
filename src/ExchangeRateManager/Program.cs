namespace ExchangeRateManager;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage(Justification = "Untestable code")]
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var startup = new Startup(builder.Environment);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app);

        await app.RunAsync();
    }
}
