using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Data.Repositories;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class LlmProviderService : ILlmProviderService
    {
        private readonly ILlmProviderRepository _providerRepository;
        private readonly ILlmModelRepository _modelRepository;
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<LlmProviderService> _logger;

        public LlmProviderService(
            ILlmProviderRepository providerRepository,
            ILlmModelRepository modelRepository,
            McpServerDbContext dbContext,
            ILogger<LlmProviderService> logger)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<LlmProvider>> GetAllProvidersAsync()
        {
            _logger.LogInformation("Getting all LLM providers");
            var providers = await _providerRepository.GetAllAsync();
            return providers.ToList(); // Convert IEnumerable to List
        }

        public async Task<LlmProvider?> GetProviderByIdAsync(int id)
        {
            _logger.LogInformation("Getting LLM provider with ID: {ProviderId}", id);
            return await _providerRepository.GetByIdAsync(id);
        }

        public async Task<LlmProvider?> GetProviderByNameAsync(string name)
        {
            _logger.LogInformation("Getting LLM provider with name: {ProviderName}", name);
            return await _providerRepository.GetProviderByNameAsync(name);
        }

        public async Task<List<LlmModel>> GetAllModelsAsync()
        {
            _logger.LogInformation("Getting all LLM models");
            var models = await _modelRepository.GetAllModelsWithProvidersAsync();
            return models.ToList(); // Convert IEnumerable to List
        }

        public async Task<List<LlmModel>> GetModelsByProviderIdAsync(int providerId)
        {
            _logger.LogInformation("Getting models for provider ID: {ProviderId}", providerId);
            var models = await _providerRepository.GetModelsForProviderAsync(providerId);
            return models.ToList(); // Convert IEnumerable to List
        }

        public async Task<LlmModel?> GetModelByIdAsync(int id)
        {
            _logger.LogInformation("Getting model with ID: {ModelId}", id);
            return await _modelRepository.GetModelWithProviderAsync(id);
        }

        public async Task<LlmModel?> GetModelByNameAsync(string name)
        {
            _logger.LogInformation("Getting model with name: {ModelName}", name);
            return await _dbContext.LlmModels
                .Include(m => m.Provider)
                .FirstOrDefaultAsync(m => m.Name == name);
        }

        public async Task<LlmModel?> GetDefaultModelForProviderAsync(int providerId)
        {
            _logger.LogInformation("Getting default model for provider ID: {ProviderId}", providerId);
            
            // LlmModel doesn't have IsDefault property, find first enabled model for the provider
            // This is a simplification - you may want to add an IsDefault column to your LlmModel table
            return await _dbContext.LlmModels
                .Include(m => m.Provider)
                .Where(m => m.ProviderId == providerId && m.IsEnabled)
                .FirstOrDefaultAsync();
        }

        public async Task<LlmProviderCredential?> GetDefaultCredentialAsync(int providerId, Guid? userId = null)
        {
            _logger.LogInformation("Getting default credential for provider ID: {ProviderId}", providerId);
            
            // First try to get user-specific default credential
            if (userId.HasValue)
            {
                var userCredential = await _dbContext.LlmProviderCredentials
                    .FirstOrDefaultAsync(c =>
                        c.ProviderId == providerId &&
                        c.UserId == userId &&
                        c.IsDefault &&
                        c.IsEnabled);

                if (userCredential != null)
                {
                    return userCredential;
                }
            }

            // Fall back to system-wide default credential
            return await _dbContext.LlmProviderCredentials
                .FirstOrDefaultAsync(c =>
                    c.ProviderId == providerId &&
                    c.UserId == null &&
                    c.IsDefault &&
                    c.IsEnabled);
        }

        public async Task<LlmProvider> AddProviderAsync(LlmProvider provider)
        {
            _logger.LogInformation("Adding new provider: {ProviderName}", provider.Name);
            
            provider.CreatedAt = DateTime.UtcNow;
            await _providerRepository.AddAsync(provider);
            await _dbContext.SaveChangesAsync();
            
            return provider;
        }

        public async Task<bool> UpdateProviderAsync(LlmProvider provider)
        {
            _logger.LogInformation("Updating provider: {ProviderName} (ID: {ProviderId})", 
                provider.Name, provider.Id);
            
            var existingProvider = await _providerRepository.GetByIdAsync(provider.Id);
            if (existingProvider == null)
            {
                _logger.LogWarning("Provider not found with ID: {ProviderId}", provider.Id);
                return false;
            }
            
            provider.UpdatedAt = DateTime.UtcNow;
            await _providerRepository.UpdateAsync(provider);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> DeleteProviderAsync(int id)
        {
            _logger.LogInformation("Deleting provider with ID: {ProviderId}", id);
            
            var provider = await _providerRepository.GetByIdAsync(id);
            if (provider == null)
            {
                _logger.LogWarning("Provider not found with ID: {ProviderId}", id);
                return false;
            }
            
            await _providerRepository.DeleteAsync(id);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }

        public async Task<LlmModel> AddModelAsync(LlmModel model)
        {
            _logger.LogInformation("Adding new model: {ModelName} for provider ID: {ProviderId}", 
                model.Name, model.ProviderId);
            
            model.CreatedAt = DateTime.UtcNow;
            await _modelRepository.AddAsync(model);
            await _dbContext.SaveChangesAsync();
            
            return model;
        }

        public async Task<bool> UpdateModelAsync(LlmModel model)
        {
            _logger.LogInformation("Updating model: {ModelName} (ID: {ModelId})", 
                model.Name, model.Id);
            
            var existingModel = await _modelRepository.GetByIdAsync(model.Id);
            if (existingModel == null)
            {
                _logger.LogWarning("Model not found with ID: {ModelId}", model.Id);
                return false;
            }
            
            model.UpdatedAt = DateTime.UtcNow;
            await _modelRepository.UpdateAsync(model);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> DeleteModelAsync(int id)
        {
            _logger.LogInformation("Deleting model with ID: {ModelId}", id);
            
            var model = await _modelRepository.GetByIdAsync(id);
            if (model == null)
            {
                _logger.LogWarning("Model not found with ID: {ModelId}", id);
                return false;
            }
            
            await _modelRepository.DeleteAsync(id);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }

        public async Task<List<string>> GetProviderTypesAsync()
        {
            _logger.LogInformation("Getting all provider types");
            
            // LlmProvider doesn't have a Type property
            // Return a predefined list of common provider types instead
            return await Task.FromResult(new List<string>
            {
                "OpenAI",
                "Anthropic",
                "Google",
                "Cohere",
                "Azure",
                "MistralAI",
                "Other"
            });
        }

        public async Task<List<string>> GetModelCapabilitiesAsync()
        {
            _logger.LogInformation("Getting all model capabilities");
            
            // For now, return a static list of common capabilities
            // In a real implementation, you would fetch this from a database table or configuration
            return await Task.FromResult(new List<string>
            {
                "chat",
                "completion",
                "embedding",
                "vision",
                "audio",
                "function-calling"
            });
        }
    }
}