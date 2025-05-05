using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Chat;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Features.Chat.Services.Interfaces
{
    /// <summary>
    /// Service for chat playground functionality
    /// </summary>
    [Obsolete("This service is being replaced by more specialized services. Use IModelService, ILlmService, and IChatStreamingService instead.")]
    public interface IChatPlaygroundService
    {
        /// <summary>
        /// Gets available models for the chat playground
        /// </summary>
        Task<List<LlmModel>> GetAvailableModelsAsync();

        /// <summary>
        /// Sends a message to the LLM and returns the response
        /// </summary>
        Task<ChatResponse> SendMessageAsync(ChatRequest request, string? username = null);

        /// <summary>
        /// Streams a response from the LLM
        /// </summary>
        Task StreamResponseAsync(string sessionId, string message, LlmModel model, Func<string, bool, Task> onChunkReceived, string? username = null);

        /// <summary>
        /// Tests database connectivity
        /// </summary>
        Task<bool> TestDatabaseConnectionAsync();
    }
}
