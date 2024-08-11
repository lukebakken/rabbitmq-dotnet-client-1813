using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Repositories.Core;
using ExchangeRateManager.Repositories.Core.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExchangeRateManager.Repositories.Extensions
{
    /// <summary>
    /// Database related Service extensions
    /// </summary>
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options
                    // Postgres database. We can use SQLServer, mongodb or any other type of database for this situation.
                    .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStrings.DatabaseConnection))

                    // Apply as no tracking with Identity resolution, so it wont create multiple entities for data in common.
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)

                    // Attach EF logs through the preconfigured logging.
                    .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            });

            services.AddDbContext<HangfireDbContext>((sp, options) =>
            {
                options
                    .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStrings.HangfireConnection));
            });

            return services;
        }

        public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder app)
        {
            app.ApplicationServices
                .GetRequiredService<IMigrationService>()
                .ApplyMigrations(true).Wait();

            return app;
        }
    }
}
