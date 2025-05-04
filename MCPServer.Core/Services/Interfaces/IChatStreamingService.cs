using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Services.Interfaces
{
    /// <summary>
    /// Service for handling chat streaming
    /// </summary>
    public interface IChatStreamingService
    {
        /// <summary>
        /// Process a streaming request
        /// </summary>
        Task<string> ProcessStreamAsync(
            string sessionId,
            string message,
            List<Message> sessionHistory,
            LlmModel? model,
            double temperature,
            int maxTokens,
            Func<string, bool, Task> onChunkReceived);

        /// <summary>
        /// Stores a streaming request for later processing
        /// </summary>
        Task StoreStreamRequestAsync(string sessionId, object request);

        /// <summary>
        /// Gets a pending streaming request for a session
        /// </summary>
        Task<object?> GetPendingStreamRequestAsync(string sessionId);

        /// <summary>
        /// Removes a pending streaming request for a session
        /// </summary>
        Task RemovePendingStreamRequestAsync(string sessionId);
    }
}