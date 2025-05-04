namespace MCPServer.Core.Models.Chat
{
    /// <summary>
    /// Response model for streaming chat operations
    /// </summary>
    public class StreamChunkResponse
    {
        /// <summary>
        /// The chunk of text from the LLM
        /// </summary>
        public string Chunk { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the last chunk
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// The session ID for the chat
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
    }
}
