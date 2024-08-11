using Microsoft.EntityFrameworkCore;

namespace ExchangeRateManager.Repositories.Base
{
    /// <summary>
    /// Abstract generic class that defines the basic tools for entity and DbCcontext interaction used by repositories.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to represent.</typeparam>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    public abstract class DataRepository<TEntity, TContext>(TContext context, DbSet<TEntity> entities)
        where TContext : DbContext
        where TEntity : class
    {
        /// <summary>
        /// The Database Context
        /// </summary>
        protected TContext Context => context;

        /// <summary>
        /// The database set of the Entity.
        /// </summary>
        protected DbSet<TEntity> Entities => entities;
    }
}