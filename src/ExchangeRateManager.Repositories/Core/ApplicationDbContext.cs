using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateManager.Repositories.Core;

/// <summary>
/// The Application Database Context that interfaces the application's data souce.
/// </summary>
/// <param name="options">Options setting up the DbContext</param>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ForexRateEntity> ForexRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assemblies = ServiceExtensions.LoadAssemblies();
        foreach (var assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
