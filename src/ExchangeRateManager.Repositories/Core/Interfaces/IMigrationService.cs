using ExchangeRateManager.Common.Interfaces.Base;

namespace ExchangeRateManager.Repositories.Core.Interfaces;

public interface IMigrationService : IService
{
    Task ApplyMigrations(bool IsStartup = false);
}