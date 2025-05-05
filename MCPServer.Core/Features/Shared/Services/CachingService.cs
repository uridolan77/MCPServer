using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Features.Shared.Services
{
    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);

            // Get or create a lock for this key to prevent multiple factory executions
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();

                // Double-check after acquiring the lock
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    _logger.LogDebug("Cache hit after lock for key: {Key}", key);
                    return cachedValue;
                }

                // Execute the factory
                var result = await factory();

                // Set cache options
                var cacheOptions = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // Default expiration of 5 minutes
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }

                // Add to cache
                _cache.Set(key, result, cacheOptions);
                _logger.LogDebug("Added to cache: {Key}", key);

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);

            // Get or create a lock for this key to prevent multiple factory executions
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            try
            {
                semaphore.Wait();

                // Double-check after acquiring the lock
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    _logger.LogDebug("Cache hit after lock for key: {Key}", key);
                    return cachedValue;
                }

                // Execute the factory
                var result = factory();

                // Set cache options
                var cacheOptions = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // Default expiration of 5 minutes
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }

                // Add to cache
                _cache.Set(key, result, cacheOptions);
                _logger.LogDebug("Added to cache: {Key}", key);

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Remove(string key)
        {
            _logger.LogDebug("Removing from cache: {Key}", key);
            _cache.Remove(key);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            var result = _cache.TryGetValue(key, out value);
            if (result)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }
            return result;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var cacheOptions = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 5 minutes
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            _cache.Set(key, value, cacheOptions);
            _logger.LogDebug("Added to cache: {Key}", key);
        }
    }
}



