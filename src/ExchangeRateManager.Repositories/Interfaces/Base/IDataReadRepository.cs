namespace ExchangeRateManager.Repositories.Interfaces.Base
{
    public interface IDataReadRepository<TEntity, TKey>
    {
        Task<TEntity?> FindByIdAsync(TKey id);
        TEntity? FindById(TKey id);
    }
}