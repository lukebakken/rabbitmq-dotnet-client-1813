using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ExchangeRateManager.Repositories.Interfaces.Base
{
    public interface IUpsertDataRepository<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Updates an existing record, otherwise adds a new entry.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>A Entry reference to the updated entity</returns>
        Task<EntityEntry<TEntity>> Upsert(TEntity entity);
    }
}