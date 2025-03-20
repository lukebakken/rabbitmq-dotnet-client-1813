using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using ExchangeRateManager.Repositories.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExchangeRateManager.Repositories.Core;

/// <summary>
/// Ensures the application of all database migrations.
/// </summary>
/// <param name="appDbContext"></param>
/// <param name="hfDbContext"></param>
/// <param name="settings"></param>
public class MigrationService(
    ApplicationDbContext appDbContext, HangfireDbContext hfDbContext, IOptions<Settings> settings) : IMigrationService, ITransient
{
    private readonly ApplicationDbContext _appDbContext = appDbContext;
    private readonly HangfireDbContext _hfDbContext = hfDbContext;
    private readonly Settings _settings = settings.Value;

    /// <summary>
    /// Applies the application migrations.
    /// </summary>
    /// <param name="IsStartup">Condition only used at startup in combination with RunMigrationsAtStartup.
    /// Leave it as is if for manual operation.</param>
    public async Task ApplyMigrations(bool IsStartup = false)
    {
        if (!EF.IsDesignTime && (!IsStartup || _settings.RunMigrationsAtStartup))
        {
            await _appDbContext.Database.MigrateAsync();
            await _appDbContext.SaveChangesAsync();

            await _hfDbContext.Database.EnsureCreatedAsync();
            await _hfDbContext.SaveChangesAsync();
        }
    }
}
