using System.Collections.Generic;
using MCPServer.Core.Models;

namespace MCPServer.API.Features.Chat.Models
{
    /// <summary>
    /// Request model for chat operations
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// The user's message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The system prompt
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// The model ID
        /// </summary>
        public int? ModelId { get; set; }

        /// <summary>
        /// The temperature
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// The maximum number of tokens
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// The chat history
        /// </summary>
        public List<Message>? History { get; set; }
    }

    /// <summary>
    /// Response model for chat operations
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// The assistant's response
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
    }
}
