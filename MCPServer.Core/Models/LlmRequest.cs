using System.Collections.Generic;

namespace MCPServer.Core.Models
{
    public class LlmMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class LlmTool
    {
        public string Type { get; set; } = "function";
        public LlmFunction Function { get; set; } = new LlmFunction();
    }

    public class LlmFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object Parameters { get; set; } = new object();
    }

    public class LlmRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<LlmMessage> Messages { get; set; } = new List<LlmMessage>();
        public double Temperature { get; set; } = 0.7;

        // OpenAI uses 'max_tokens' instead of 'maxTokens'
        public int Max_tokens { get; set; } = 2000;

        public bool Stream { get; set; } = false;
        public List<LlmTool>? Tools { get; set; }
    }
}
