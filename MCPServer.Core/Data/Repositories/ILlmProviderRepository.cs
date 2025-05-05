using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository interface for LlmProvider entities
    /// </summary>
    public interface ILlmProviderRepository : IRepository<LlmProvider>
    {
        /// <summary>
        /// Gets all enabled providers
        /// </summary>
        Task<IEnumerable<LlmProvider>> GetEnabledProvidersAsync();
        
        /// <summary>
        /// Gets a provider by name
        /// </summary>
        Task<LlmProvider?> GetProviderByNameAsync(string name);
        
        /// <summary>
        /// Gets all models for a provider
        /// </summary>
        Task<IEnumerable<LlmModel>> GetModelsForProviderAsync(int providerId);
        
        /// <summary>
        /// Gets all enabled models for a provider
        /// </summary>
        Task<IEnumerable<LlmModel>> GetEnabledModelsForProviderAsync(int providerId);
    }
}

