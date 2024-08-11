using Microsoft.EntityFrameworkCore;

namespace ExchangeRateManager.Repositories.Core;

/// <summary>
/// Placeholder DbContext for automatic creation of Hangfire's database.
/// </summary>
public class HangfireDbContext(DbContextOptions options) : DbContext(options) { }