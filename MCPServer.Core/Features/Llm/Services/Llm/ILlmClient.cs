using System;
using System.Threading.Tasks;
using MCPServer.Core.Models;

namespace MCPServer.Core.Features.Llm.Services.Llm
{
    /// <summary>
    /// Interface for individual LLM clients that handle communication with specific LLM providers
    /// </summary>
    public interface ILlmClient
    {
        /// <summary>
        /// Send a request to the LLM provider and return the response
        /// </summary>
        Task<LlmResponse> SendRequestAsync(LlmRequest request);

        /// <summary>
        /// Stream a response from the LLM provider
        /// </summary>
        Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived);
    }
}
