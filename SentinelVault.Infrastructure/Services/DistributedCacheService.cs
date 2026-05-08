using SentinelVault.Application.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SentinelVault.Infrastructure.Services
{
    /// <summary>Distributed cache service using the configured provider (currently Memory Cache).</summary>
    public class DistributedCacheService(IDistributedCache cache) : ICacheService
    {
        /// <summary>Gets a cached value by key.</summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await cache.GetStringAsync(key);
            return data == null ? default : JsonSerializer.Deserialize<T>(data);
        }

        /// <summary>Sets a cached value with optional expiration.</summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            };
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }

        /// <summary>Removes a cached value by key.</summary>
        public async Task RemoveAsync(string key)
        {
            await cache.RemoveAsync(key);
        }
    }
}
