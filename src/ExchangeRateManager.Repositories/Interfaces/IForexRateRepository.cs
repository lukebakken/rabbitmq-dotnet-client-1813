using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Repositories.Entities;
using ExchangeRateManager.Repositories.Interfaces.Base;

namespace ExchangeRateManager.Repositories.Interfaces
{
    public interface IForexRateRepository :
        IDataReadRepository<ForexRateEntity, ForexRateKey>,
        IUpsertDataRepository<ForexRateEntity>,
        IRepository
    { }
}