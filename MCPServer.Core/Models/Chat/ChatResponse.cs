using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Models.Chat
{
    /// <summary>
    /// Response model for chat operations
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// The response message from the LLM
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The session ID for the chat
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        
        /// <summary>
        /// The model used for the response
        /// </summary>
        public LlmModel? Model { get; set; }
        
        /// <summary>
        /// The processing time in milliseconds
        /// </summary>
        public long ProcessingTime { get; set; }
    }
}
