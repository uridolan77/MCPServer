using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Services.Interfaces
{
    // Interface to maintain compatibility with existing code
    public interface ILlmProviderService
    {
        /// <summary>
        /// Gets all providers
        /// </summary>
        Task<List<LlmProvider>> GetAllProvidersAsync();
        
        /// <summary>
        /// Gets all models
        /// </summary>
        Task<List<LlmModel>> GetAllModelsAsync();
        
        /// <summary>
        /// Gets a provider by ID
        /// </summary>
        Task<LlmProvider?> GetProviderByIdAsync(int id);
        
        /// <summary>
        /// Gets a provider by name
        /// </summary>
        Task<LlmProvider?> GetProviderByNameAsync(string name);
        
        /// <summary>
        /// Gets models by provider ID
        /// </summary>
        Task<List<LlmModel>> GetModelsByProviderIdAsync(int providerId);
        
        /// <summary>
        /// Gets a model by ID
        /// </summary>
        Task<LlmModel?> GetModelByIdAsync(int id);
        
        /// <summary>
        /// Gets a model by name
        /// </summary>
        Task<LlmModel?> GetModelByNameAsync(string name);
        
        /// <summary>
        /// Gets the default model for a provider
        /// </summary>
        Task<LlmModel?> GetDefaultModelForProviderAsync(int providerId);
        
        /// <summary>
        /// Adds a new provider
        /// </summary>
        Task<LlmProvider> AddProviderAsync(LlmProvider provider);
        
        /// <summary>
        /// Updates an existing provider
        /// </summary>
        Task<bool> UpdateProviderAsync(LlmProvider provider);
        
        /// <summary>
        /// Deletes a provider
        /// </summary>
        Task<bool> DeleteProviderAsync(int id);
        
        /// <summary>
        /// Adds a new model
        /// </summary>
        Task<LlmModel> AddModelAsync(LlmModel model);
        
        /// <summary>
        /// Updates an existing model
        /// </summary>
        Task<bool> UpdateModelAsync(LlmModel model);
        
        /// <summary>
        /// Deletes a model
        /// </summary>
        Task<bool> DeleteModelAsync(int id);
        
        /// <summary>
        /// Gets all available provider types
        /// </summary>
        Task<List<string>> GetProviderTypesAsync();
        
        /// <summary>
        /// Gets all model capabilities
        /// </summary>
        Task<List<string>> GetModelCapabilitiesAsync();
    }
}