using AutoMapper;
using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using ExchangeRateManager.Repositories.Base;
using ExchangeRateManager.Repositories.Core;
using ExchangeRateManager.Repositories.Entities;
using ExchangeRateManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Distributed;

namespace ExchangeRateManager.Repositories
{
    /// <summary>
    /// CRUD operations for the ForexRateEntity
    /// </summary>
    public class ForexRateRepository :
        CachedDataReadRepository<ForexRateKey, ForexRateEntity, ApplicationDbContext>,
        IForexRateRepository, IScoped
    {
        private readonly IMapper _mapper;

        /// <summary>
        /// Will use this constructor if there is a cache provider
        /// </summary>
        /// <param name="dbContext">The db context</param>
        /// <param name="mapper">The AutoMapper</param>
        /// <param name="distributedCache">An instance of the distributed cache provider.</param>
        public ForexRateRepository(
            ApplicationDbContext dbContext, IMapper mapper,
            IDistributedCache distributedCache)
            : base(dbContext, dbContext.ForexRates, distributedCache)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Will use this constructor if there is no cache provider
        /// </summary>
        /// <param name="dbContext">The db context</param>
        /// <param name="mapper">The AutoMapper</param>
        public ForexRateRepository(
            ApplicationDbContext dbContext, IMapper mapper)
            : base(dbContext, dbContext.ForexRates)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Checks if the entity exists
        /// </summary>
        /// <param name="forexRateEntity">The entity</param>
        /// <returns>True if exists, otherwise false.</returns>
        private async Task<bool> Exists(ForexRateEntity forexRateEntity)
            => await Entities.ContainsAsync(forexRateEntity);

        public async Task<EntityEntry<ForexRateEntity>> Upsert(ForexRateEntity entity)
        {
            EntityEntry<ForexRateEntity> result;
            if (await Exists(entity))
            {
                result = Entities.Update(entity);
            }
            else
            {
                result = Entities.Add(entity);
            }

            await Context.SaveChangesAsync();

            if (_distributedCache != null)
            {
                await _distributedCache.SetStringAsync(
                    _mapper.Map<ForexRateKey>(entity).ToJson(),
                    entity.ToJson());
            }

            return result;
        }
    }
}
