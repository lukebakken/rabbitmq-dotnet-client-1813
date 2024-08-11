using ExchangeRateManager.Repositories.Interfaces.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ExchangeRateManager.Repositories.Base;

/// <summary>
/// Abstract class that defines read operations to the database.
/// </summary>
/// <typeparam name="TKey">Then Entity key type. Use classes or tuples for composite keys.</typeparam>
/// <typeparam name="TEntity">The Entity type.</typeparam>
/// <typeparam name="TContext">The database context type.</typeparam>

public abstract class DataReadRepository<TKey, TEntity, TContext>(
    TContext context, DbSet<TEntity> dbSet) :
        DataRepository<TEntity, TContext>(context, dbSet),
        IDataReadRepository<TEntity, TKey>
        where TContext : DbContext
        where TEntity : class
{
    /// <summary>
    /// Finds the entity record by its primary key.
    /// </summary>
    /// <param name="id">The primary key or unique identifier.</param>
    /// <returns>the entity record.</returns>
    public virtual TEntity? FindById(TKey id) =>
        Entities.Find(GetKeys(id));

    /// <summary>
    /// Finds the entity record by its primary key asynchronously.
    /// </summary>
    /// <param name="id">The primary key or unique identifier.</param>
    /// <returns>the entity record.</returns>
    public virtual async Task<TEntity?> FindByIdAsync(TKey id) =>
        await Entities.FindAsync(GetKeys(id));

    /// <summary>
    /// Discovers the properties or the value from an object as keys for Find and FindAsync.
    /// </summary>
    /// <param name="id">The key object to parse.</param>
    /// <returns>the keys in an ordered array.</returns>
    private static object?[] GetKeys(TKey id)
    {
        var type = typeof(TKey);
        bool isPrimitiveType = type.IsPrimitive || type.IsValueType || type == typeof(string);
        if (isPrimitiveType)
        {
            return [id];
        }

        var props = id?.GetType().GetProperties().OrderBy(p => p.GetCustomAttribute<ColumnAttribute>()?.Order ?? 0);
        object?[] keys;
        if ((props?.Count() ?? 0) > 0)
        {
            keys = props!.Select(x => x.GetValue(id)).ToArray();
        }
        else
        {
            keys = [id];
        }

        return keys;
    }
}