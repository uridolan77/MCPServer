using System;
using System.Text.Json.Serialization;

namespace MCPServer.Core.Models.Llm
{
    public class LlmModel
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty; // The ID used by the provider (e.g., "gpt-4o")
        public string Description { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 4000;
        public int ContextWindow { get; set; } = 8000;
        public bool SupportsStreaming { get; set; } = true;
        public bool SupportsVision { get; set; } = false;
        public bool SupportsTools { get; set; } = false;
        public decimal CostPer1KInputTokens { get; set; } = 0;
        public decimal CostPer1KOutputTokens { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [JsonIgnore] // Break circular reference
        public virtual LlmProvider Provider { get; set; } = null!;
    }
}
