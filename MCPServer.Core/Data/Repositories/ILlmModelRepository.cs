using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository interface for LlmModel entities
    /// </summary>
    public interface ILlmModelRepository : IRepository<LlmModel>
    {
        /// <summary>
        /// Gets all enabled models
        /// </summary>
        Task<IEnumerable<LlmModel>> GetEnabledModelsAsync();
        
        /// <summary>
        /// Gets a model by its provider-specific model ID
        /// </summary>
        Task<LlmModel?> GetModelByProviderModelIdAsync(string modelId);
        
        /// <summary>
        /// Gets a model with its provider information
        /// </summary>
        Task<LlmModel?> GetModelWithProviderAsync(int id);
        
        /// <summary>
        /// Gets all models with their provider information
        /// </summary>
        Task<IEnumerable<LlmModel>> GetAllModelsWithProvidersAsync();
        
        /// <summary>
        /// Gets all enabled models with their provider information
        /// </summary>
        Task<IEnumerable<LlmModel>> GetEnabledModelsWithProvidersAsync();
    }
}

