using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Services.Interfaces;

namespace MCPServer.Core.Services
{
    /// <summary>
    /// Service for caching data in memory
    /// </summary>
    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingService> _logger;

        public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a value from the cache or creates it using the factory function if it doesn't exist
        /// </summary>
        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return _cache.GetOrCreate(key, entry =>
            {
                if (expiration.HasValue)
                {
                    entry.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // Default expiration of 5 minutes
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return factory();
            });
        }

        /// <summary>
        /// Gets a value from the cache or creates it using the async factory function if it doesn't exist
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            var value = await factory();

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 5 minutes
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            _cache.Set(key, value, cacheEntryOptions);
            return value;
        }

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            _logger.LogDebug("Removing cache entry for key: {Key}", key);
            _cache.Remove(key);
        }

        /// <summary>
        /// Checks if a key exists in the cache
        /// </summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            var exists = _cache.TryGetValue(key, out value);
            _logger.LogDebug("Cache {Result} for key: {Key}", exists ? "hit" : "miss", key);
            return exists;
        }
    }
}
