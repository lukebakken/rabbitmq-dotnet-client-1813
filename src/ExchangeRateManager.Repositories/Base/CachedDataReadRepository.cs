using ExchangeRateManager.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ExchangeRateManager.Repositories.Base;

/// <summary>
/// Abstract class that defines cached read operations to the database.
/// </summary>
/// <typeparam name="TKey">Then Entity key type. Use classes or tuples for composite keys.</typeparam>
/// <typeparam name="TEntity">The Entity type.</typeparam>
/// <typeparam name="TContext">The database context type.</typeparam>
public abstract class CachedDataReadRepository<TKey, TEntity, TContext>(
    TContext context, DbSet<TEntity> dbSet, IDistributedCache? distributedCache = null) :
        DataReadRepository<TKey, TEntity, TContext>(context, dbSet)
        where TContext : DbContext
        where TEntity : class
{

    protected readonly IDistributedCache? _distributedCache = distributedCache;

    public override TEntity? FindById(TKey id)
    {
        var cached = _distributedCache?.GetString(id.ToJson());

        return string.IsNullOrWhiteSpace(cached)
            ? base.FindById(id) : cached.FromJson<TEntity>();
    }

    public override async Task<TEntity?> FindByIdAsync(TKey id)
    {
        string? cached = string.Empty;
        if (_distributedCache != default)
        {
            cached = await _distributedCache.GetStringAsync(id.ToJson());
        }

        return string.IsNullOrWhiteSpace(cached)
            ? await base.FindByIdAsync(id) : cached.FromJson<TEntity>();
    }
}