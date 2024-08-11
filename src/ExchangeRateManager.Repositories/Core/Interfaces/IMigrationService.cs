using ExchangeRateManager.Common.Interfaces.Base;

namespace ExchangeRateManager.Repositories.Core.Core;

public interface IMigrationService : IService
{
    Task ApplyMigrations(bool IsStartup = false);
}