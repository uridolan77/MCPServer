using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class ModelService : IModelService
    {
        private readonly IDbContextFactory<McpServerDbContext> _dbContextFactory;
        private readonly ILlmProviderService _llmProviderService;
        private readonly ILogger<ModelService> _logger;

        public ModelService(
            IDbContextFactory<McpServerDbContext> dbContextFactory,
            ILlmProviderService llmProviderService,
            ILogger<ModelService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _llmProviderService = llmProviderService;
            _logger = logger;
        }

        public async Task<List<LlmModel>> GetAvailableModelsAsync()
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            try
            {
                _logger.LogInformation("Getting available models");

                // Try to get models directly from the database first with eager loading for Provider
                var dbModels = await dbContext.LlmModels
                    .Include(m => m.Provider)
                    .Where(m => m.IsEnabled)
                    .ToListAsync();

                // Debug log each model and its provider
                foreach (var model in dbModels)
                {
                    _logger.LogInformation("Model: {ModelName}, Provider: {ProviderName}", 
                        model.Name, 
                        model.Provider?.Name ?? "No Provider");
                }

                _logger.LogInformation("Found {Count} models directly from database", dbModels.Count);

                if (dbModels.Count > 0)
                {
                    // Make sure we're not returning null for any provider references
                    foreach (var model in dbModels)
                    {
                        if (model.Provider == null)
                        {
                            // If provider is missing, try to look it up
                            var provider = await dbContext.LlmProviders.FindAsync(model.ProviderId);
                            if (provider != null)
                            {
                                model.Provider = provider;
                                _logger.LogInformation("Fixed missing provider reference for model {ModelName}", model.Name);
                            }
                        }
                    }
                    return dbModels;
                }

                // If no models in database, try the service
                _logger.LogInformation("No models found in database, trying service...");

                // Get all models from the provider service
                var models = await _llmProviderService.GetAllModelsAsync();
                _logger.LogInformation("Found {Count} models from service", models.Count);

                // Filter to only enabled models
                var enabledModels = models.Where(m => m.IsEnabled).ToList();
                _logger.LogInformation("Found {Count} enabled models from service", enabledModels.Count);

                // If no models are found, check if any providers exist
                if (enabledModels.Count == 0)
                {
                    _logger.LogWarning("No enabled models found, checking if any providers exist");

                    // Check if any providers exist
                    var providers = await dbContext.LlmProviders.ToListAsync();
                    _logger.LogInformation("Found {Count} providers in database", providers.Count);

                    if (providers.Count == 0)
                    {
                        _logger.LogWarning("No providers found in database. Database may not be initialized properly.");

                        // Create a dummy model for testing
                        var dummyModel = new LlmModel
                        {
                            Id = 1,
                            Name = "Test Model",
                            ModelId = "test-model",
                            Description = "This is a test model for debugging",
                            IsEnabled = true,
                            MaxTokens = 2000,
                            ContextWindow = 8000,
                            ProviderId = 1,
                            Provider = new LlmProvider
                            {
                                Id = 1,
                                Name = "Test Provider",
                                DisplayName = "Test Provider",
                                IsEnabled = true
                            }
                        };

                        return new List<LlmModel> { dummyModel };
                    }
                }

                return enabledModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available models: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<LlmModel>> GetEnabledModelsAsync()
        {
            var allModels = await GetAvailableModelsAsync();
            return allModels.Where(m => m.IsEnabled).ToList();
        }

        public async Task<LlmModel?> GetModelByIdAsync(int modelId)
        {
            try
            {
                using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                
                var model = await dbContext.LlmModels
                    .Include(m => m.Provider)
                    .FirstOrDefaultAsync(m => m.Id == modelId);
                
                if (model != null)
                {
                    _logger.LogInformation("Found model by ID {ModelId}: {ModelName}", modelId, model.Name);
                    return model;
                }
                
                // If not found through direct query, try the provider service
                return await _llmProviderService.GetModelByIdAsync(modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model by ID {ModelId}", modelId);
                return null;
            }
        }

        public async Task<LlmProvider?> GetProviderForModelAsync(LlmModel model)
        {
            if (model.Provider != null)
            {
                return model.Provider;
            }

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            try
            {
                if (model.ProviderId > 0)
                {
                    var provider = await dbContext.LlmProviders.FindAsync(model.ProviderId);
                    if (provider != null)
                    {
                        _logger.LogInformation("Found provider for model {ModelName}: {ProviderName}", 
                            model.Name, provider.Name);
                        
                        // Update the model's Provider property
                        model.Provider = provider;
                        return provider;
                    }
                }
                
                _logger.LogWarning("Could not find provider for model {ModelName} with ProviderId {ProviderId}", 
                    model.Name, model.ProviderId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider for model {ModelName}", model.Name);
                return null;
            }
        }
    }
}