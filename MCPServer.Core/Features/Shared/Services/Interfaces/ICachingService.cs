using System;
using System.Threading.Tasks;

namespace MCPServer.Core.Features.Shared.Services.Interfaces
{
    /// <summary>
    /// Interface for caching service
    /// </summary>
    public interface ICachingService
    {
        /// <summary>
        /// Gets a value from the cache or creates it using the factory function if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The type of the value to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="factory">The factory function to create the value if it doesn't exist in the cache</param>
        /// <param name="expiration">Optional expiration time for the cache entry</param>
        /// <returns>The cached or newly created value</returns>
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);

        /// <summary>
        /// Gets a value from the cache or creates it using the async factory function if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The type of the value to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="factory">The async factory function to create the value if it doesn't exist in the cache</param>
        /// <param name="expiration">Optional expiration time for the cache entry</param>
        /// <returns>The cached or newly created value</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        /// <param name="key">The cache key to remove</param>
        void Remove(string key);

        /// <summary>
        /// Checks if a key exists in the cache and retrieves its value
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="value">The retrieved value if found</param>
        /// <returns>True if the key exists in the cache, false otherwise</returns>
        bool TryGetValue<T>(string key, out T value);
    }
}
