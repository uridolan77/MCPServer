using System.Collections.Generic;

namespace MCPServer.Core.Models.Chat
{
    /// <summary>
    /// Request model for chat operations
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// The message to send to the LLM
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The session ID for the chat
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The chat history
        /// </summary>
        public List<Message>? History { get; set; }

        /// <summary>
        /// The model ID to use
        /// </summary>
        public int? ModelId { get; set; }

        /// <summary>
        /// The temperature to use for generation
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// The maximum number of tokens to generate
        /// </summary>
        public int? MaxTokens { get; set; }
        
        /// <summary>
        /// The system prompt to use for the chat
        /// </summary>
        public string? SystemPrompt { get; set; }
    }
}
