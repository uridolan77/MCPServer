using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Features.Models.Services.Interfaces;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Models.Services
{
    public class ModelService : IModelService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<ModelService> _logger;
        private readonly ICachingService _cachingService;

        public ModelService(
            McpServerDbContext dbContext,
            ILogger<ModelService> logger,
            ICachingService cachingService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cachingService = cachingService;
        }

        public async Task<List<LlmModel>> GetAllModelsAsync()
        {
            try
            {
                // Try to get from cache first
                string cacheKey = "AllModels";
                if (_cachingService.TryGetValue(cacheKey, out List<LlmModel> cachedModels))
                {
                    return cachedModels;
                }

                // If not in cache, get from database
                var models = await _dbContext.LlmModels
                    .Include(m => m.Provider)
                    .ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, models, TimeSpan.FromMinutes(5));

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all models");
                throw;
            }
        }

        public async Task<LlmModel?> GetModelByIdAsync(int id)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Model_{id}";
                if (_cachingService.TryGetValue(cacheKey, out LlmModel cachedModel))
                {
                    return cachedModel;
                }

                // If not in cache, get from database
                var model = await _dbContext.LlmModels
                    .Include(m => m.Provider)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Cache the result if found
                if (model != null)
                {
                    _cachingService.Set(cacheKey, model, TimeSpan.FromMinutes(5));
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model with ID {ModelId}", id);
                throw;
            }
        }

        public async Task<List<LlmModel>> GetModelsByProviderIdAsync(int providerId)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Models_Provider_{providerId}";
                if (_cachingService.TryGetValue(cacheKey, out List<LlmModel> cachedModels))
                {
                    return cachedModels;
                }

                // If not in cache, get from database
                var models = await _dbContext.LlmModels
                    .Include(m => m.Provider)
                    .Where(m => m.ProviderId == providerId)
                    .ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, models, TimeSpan.FromMinutes(5));

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models for provider with ID {ProviderId}", providerId);
                throw;
            }
        }

        public async Task<LlmModel> AddModelAsync(LlmModel model)
        {
            try
            {
                // Set created timestamp
                model.CreatedAt = DateTime.UtcNow;

                // Add to database
                _dbContext.LlmModels.Add(model);
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllModels");
                _cachingService.Remove($"Models_Provider_{model.ProviderId}");
                _cachingService.Remove("EnabledModels");

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding model {ModelName}", model.Name);
                throw;
            }
        }

        public async Task<bool> UpdateModelAsync(LlmModel model)
        {
            try
            {
                // Find the model
                var existingModel = await _dbContext.LlmModels.FindAsync(model.Id);
                if (existingModel == null)
                {
                    return false;
                }

                // Update properties
                existingModel.Name = model.Name;
                existingModel.ModelId = model.ModelId;
                existingModel.Description = model.Description;
                existingModel.MaxTokens = model.MaxTokens;
                existingModel.CostPer1KInputTokens = model.CostPer1KInputTokens;
                existingModel.CostPer1KOutputTokens = model.CostPer1KOutputTokens;
                existingModel.IsEnabled = model.IsEnabled;
                existingModel.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllModels");
                _cachingService.Remove($"Model_{model.Id}");
                _cachingService.Remove($"Models_Provider_{model.ProviderId}");
                _cachingService.Remove("EnabledModels");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model with ID {ModelId}", model.Id);
                throw;
            }
        }

        public async Task<bool> DeleteModelAsync(int id)
        {
            try
            {
                // Find the model
                var model = await _dbContext.LlmModels.FindAsync(id);
                if (model == null)
                {
                    return false;
                }

                // Delete the model
                _dbContext.LlmModels.Remove(model);
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllModels");
                _cachingService.Remove($"Model_{id}");
                _cachingService.Remove($"Models_Provider_{model.ProviderId}");
                _cachingService.Remove("EnabledModels");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model with ID {ModelId}", id);
                throw;
            }
        }

        public async Task<List<LlmModel>> GetEnabledModelsAsync()
        {
            try
            {
                // Try to get from cache first
                string cacheKey = "EnabledModels";
                if (_cachingService.TryGetValue(cacheKey, out List<LlmModel> cachedModels))
                {
                    return cachedModels;
                }

                // If not in cache, get from database
                var models = await _dbContext.LlmModels
                    .Include(m => m.Provider)
                    .Where(m => m.IsEnabled && m.Provider.IsEnabled)
                    .ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, models, TimeSpan.FromMinutes(5));

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled models");
                throw;
            }
        }

        public async Task<LlmModel?> GetModelByModelIdAsync(string modelId)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Model_ModelId_{modelId}";
                if (_cachingService.TryGetValue(cacheKey, out LlmModel cachedModel))
                {
                    return cachedModel;
                }

                // If not in cache, get from database
                var model = await _dbContext.LlmModels
                    .Include(m => m.Provider)
                    .FirstOrDefaultAsync(m => m.ModelId == modelId);

                // Cache the result if found
                if (model != null)
                {
                    _cachingService.Set(cacheKey, model, TimeSpan.FromMinutes(5));
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model with model ID {ModelId}", modelId);
                throw;
            }
        }
    }
}
