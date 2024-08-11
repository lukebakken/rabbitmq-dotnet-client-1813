namespace ExchangeRateManager;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage(Justification = "Untestable code")]
public class Program
{
    //TODO List:
    // - Improve unit and implement functional tests
    // - Use serilog, improve and customize logging.
    // - Send logs to a logging service or store them into a table.
    // - Add authentication/Authorization layers and use JWT tokens
    // - The HTTPS certificate is only used for example purposes and safe local developlment. If the service is behind
    //   a gateway like nginx we can disable this.
    // - If the services will only use small clumps of star or snowflake data, change Connection to use a
    //   NoSQL database (Ex: MongoDB)
    // - Implement pipelines for deployment into another environments. Improve and test the compose script
    //   (docker-compose.override.yml) for other environments as is is just an example mockup.
    //   Adopt CI/CD tecnologies like jenkins, gitlab pipelines, azure devops pipelines, terraform, octopus
    // - Redis/Valkey allows to have persistent shared cache. If the service does not require any
    //   kind of relational data from the time being, consider using a shared cache only. No ORM like EF is required.
    // - Consider switch RabbitMQ.Client to MassTransit Framework, if consumer operations are needed in the future
    //   or requirement for better support for asynchronous operations https://masstransit.io/quick-starts/rabbitmq
    // - Use TLS connection to access docker remotely. Current commands exposes the docker host cli. See initializeDocker.sh
    //   https://stackoverflow.com/questions/63416280/how-to-expose-docker-tcp-socket-on-wsl2-wsl-installed-docker-not-docker-deskt
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
