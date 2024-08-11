using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Hanfire;
using ExchangeRateManager.Repositories.Extensions;
using Hangfire;
using Hangfire.PostgreSql;

namespace ExchangeRateManager;

/// <summary>
/// Startup component that setups the application configuration and
/// loads and registers all dependencies.
/// </summary>
public class Startup(IWebHostEnvironment environment)
{
    private readonly IWebHostEnvironment _environment = environment;

    /// <summary>
    ///Adds and configures services to the service collection.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Load appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appSettings.{_environment.EnvironmentName}.json", false, true)
            .AddEnvironmentVariables()
            .Build();

        services.Configure<Settings>(configuration, (BinderOptions o) => o.BindNonPublicProperties = true);
        Settings settings = configuration.Get<Settings>()!;

        // Add global exception handler;
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Add app service dependencies
        services.AddServiceCollectionExtensions();
        services.AddDatabaseServices();

        // Add Hangfire services.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage((options) => options.UseNpgsqlConnection(
                configuration.GetConnectionString(ConnectionStrings.HangfireConnection))));

        // Add RabbitMQ Connection Factory
        services.AddRabbitMqConnectionFactory(configuration);

        // Add the hangfire processing server as IHostedService
        services.AddHangfireServer();

        // Add cache service
        services.AddCache(configuration, settings);

        // Load controllers
        services.AddControllers();

        // Ensure HTTPS redirection
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        });

        // Add Swagger components
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = $"{nameof(ExchangeRateManager)} API",
                Version = "v1",
                Description = "<a href=\"/hangfire\">Hangfire</a>"
            });
        });
    }

    /// <summary>
    /// Configures the HTTP request pipeline.
    /// </summary>
    /// <returns>The WebApplication ready to run.</returns>
    public void Configure(IApplicationBuilder app)
    {
        // Use the global exception handler
        app.UseExceptionHandler("/error");

        // Apply migrations
        app.ApplyMigrations();


        // Enforce HTTPS
        app.UseHttpsRedirection();

        // Use swagger
        app.UseSwagger();
        app.UseSwaggerUI();

        // Use Hangfire UI
        app.UseHangfireDashboard(options: new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()],
            AppPath = "/swagger"
        });

        // Server setup
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // Detailed error handling on lower level environments
        if (!_environment.IsHighLevel())
        {
            app.UseHsts();
        }
    }
}
