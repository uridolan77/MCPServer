using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Features.Llm.Services.Llm
{
    /// <summary>
    /// Interface for LLM provider factories that create provider-specific implementations
    /// </summary>
    public interface ILlmProviderFactory
    {
        /// <summary>
        /// Get the name of the LLM provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Check if this factory can handle the specified provider
        /// </summary>
        bool CanHandle(string providerName);

        /// <summary>
        /// Create an LLM client based on the provider, model, and credentials
        /// </summary>
        Task<ILlmClient> CreateClientAsync(LlmProvider provider, LlmModel model, LlmProviderCredential credential);
    }
}
