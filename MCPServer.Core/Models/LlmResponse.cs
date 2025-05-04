using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCPServer.Core.Models
{
    public class LlmResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<LlmChoice> Choices { get; set; } = new List<LlmChoice>();
        public LlmUsage Usage { get; set; } = new LlmUsage();
    }

    public class LlmChoice
    {
        public int Index { get; set; }
        public LlmResponseMessage? Message { get; set; }
        [JsonPropertyName("delta")]
        public LlmResponseMessage? Delta { get; set; }
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class LlmResponseMessage
    {
        public string Role { get; set; } = string.Empty;
        public string? Content { get; set; }
        [JsonPropertyName("tool_calls")]
        public List<LlmToolCall>? ToolCalls { get; set; }
    }

    public class LlmToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "function";
        public LlmFunctionCall Function { get; set; } = new LlmFunctionCall();
    }

    public class LlmFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
    }

    public class LlmUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
