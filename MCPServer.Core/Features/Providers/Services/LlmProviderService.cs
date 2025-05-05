using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Features.Providers.Services.Interfaces;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Providers.Services
{
    public class LlmProviderService : ILlmProviderService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<LlmProviderService> _logger;
        private readonly ICachingService _cachingService;

        public LlmProviderService(
            McpServerDbContext dbContext,
            ILogger<LlmProviderService> logger,
            ICachingService cachingService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cachingService = cachingService;
        }

        public async Task<List<LlmProvider>> GetAllProvidersAsync()
        {
            try
            {
                // Try to get from cache first
                string cacheKey = "AllProviders";
                if (_cachingService.TryGetValue(cacheKey, out List<LlmProvider> cachedProviders))
                {
                    return cachedProviders;
                }

                // If not in cache, get from database
                var providers = await _dbContext.LlmProviders.ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, providers, TimeSpan.FromMinutes(5));

                return providers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                throw;
            }
        }

        public async Task<LlmProvider?> GetProviderByIdAsync(int id)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Provider_{id}";
                if (_cachingService.TryGetValue(cacheKey, out LlmProvider cachedProvider))
                {
                    return cachedProvider;
                }

                // If not in cache, get from database
                var provider = await _dbContext.LlmProviders.FindAsync(id);

                // Cache the result if found
                if (provider != null)
                {
                    _cachingService.Set(cacheKey, provider, TimeSpan.FromMinutes(5));
                }

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider with ID {ProviderId}", id);
                throw;
            }
        }

        public async Task<LlmProvider?> GetProviderByNameAsync(string name)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Provider_Name_{name}";
                if (_cachingService.TryGetValue(cacheKey, out LlmProvider cachedProvider))
                {
                    return cachedProvider;
                }

                // If not in cache, get from database
                var provider = await _dbContext.LlmProviders
                    .FirstOrDefaultAsync(p => p.Name == name);

                // Cache the result if found
                if (provider != null)
                {
                    _cachingService.Set(cacheKey, provider, TimeSpan.FromMinutes(5));
                }

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider with name {ProviderName}", name);
                throw;
            }
        }

        public async Task<LlmProvider> AddProviderAsync(LlmProvider provider)
        {
            try
            {
                // Set created timestamp
                provider.CreatedAt = DateTime.UtcNow;

                // Add to database
                _dbContext.LlmProviders.Add(provider);
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllProviders");

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding provider {ProviderName}", provider.Name);
                throw;
            }
        }

        public async Task<bool> UpdateProviderAsync(LlmProvider provider)
        {
            try
            {
                // Find the provider
                var existingProvider = await _dbContext.LlmProviders.FindAsync(provider.Id);
                if (existingProvider == null)
                {
                    return false;
                }

                // Update properties
                existingProvider.Name = provider.Name;
                existingProvider.DisplayName = provider.DisplayName;
                existingProvider.Description = provider.Description;
                existingProvider.ApiEndpoint = provider.ApiEndpoint;
                existingProvider.AuthType = provider.AuthType;
                existingProvider.ConfigSchema = provider.ConfigSchema;
                existingProvider.IsEnabled = provider.IsEnabled;
                existingProvider.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllProviders");
                _cachingService.Remove($"Provider_{provider.Id}");
                _cachingService.Remove($"Provider_Name_{provider.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider with ID {ProviderId}", provider.Id);
                throw;
            }
        }

        public async Task<bool> DeleteProviderAsync(int id)
        {
            try
            {
                // Find the provider
                var provider = await _dbContext.LlmProviders.FindAsync(id);
                if (provider == null)
                {
                    return false;
                }

                // Check if there are any models using this provider
                bool hasModels = await _dbContext.LlmModels.AnyAsync(m => m.ProviderId == id);
                if (hasModels)
                {
                    _logger.LogWarning("Cannot delete provider with ID {ProviderId} because it has associated models", id);
                    return false;
                }

                // Delete the provider
                _dbContext.LlmProviders.Remove(provider);
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllProviders");
                _cachingService.Remove($"Provider_{id}");
                _cachingService.Remove($"Provider_Name_{provider.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider with ID {ProviderId}", id);
                throw;
            }
        }

        public async Task<List<LlmProvider>> GetEnabledProvidersAsync()
        {
            try
            {
                // Try to get from cache first
                string cacheKey = "EnabledProviders";
                if (_cachingService.TryGetValue(cacheKey, out List<LlmProvider> cachedProviders))
                {
                    return cachedProviders;
                }

                // If not in cache, get from database
                var providers = await _dbContext.LlmProviders
                    .Where(p => p.IsEnabled)
                    .ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, providers, TimeSpan.FromMinutes(5));

                return providers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled providers");
                throw;
            }
        }
    }
}
