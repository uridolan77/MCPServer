namespace MCPServer.Core.Models.Chat
{
    /// <summary>
    /// Message model for LLM requests
    /// </summary>
    public class LlmMessage
    {
        /// <summary>
        /// The role of the message sender (e.g., "user", "assistant", "system")
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The content of the message
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
