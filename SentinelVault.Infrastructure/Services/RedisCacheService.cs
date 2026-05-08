using SentinelVault.Application.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SentinelVault.Infrastructure.Services
{
    /// <summary>Distributed cache service using Redis with fallback for missing/down server.</summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly Microsoft.Extensions.Logging.ILogger<RedisCacheService> _logger;

        public RedisCacheService(IDistributedCache cache, Microsoft.Extensions.Logging.ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>Gets a cached value by key.</summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                return data == null ? default : JsonSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache is unavailable. Skipping GetAsync for key: {Key}", key);
                return default;
            }
        }

        /// <summary>Sets a cached value with optional expiration.</summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
                };
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache is unavailable. Skipping SetAsync for key: {Key}", key);
            }
        }

        /// <summary>Removes a cached value by key.</summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache is unavailable. Skipping RemoveAsync for key: {Key}", key);
            }
        }
    }
}
