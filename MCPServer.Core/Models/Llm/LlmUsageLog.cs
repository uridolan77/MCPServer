using System;

namespace MCPServer.Core.Models.Llm
{
    public class LlmUsageLog
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public int? CredentialId { get; set; }
        public Guid? UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal EstimatedCost { get; set; }
        public bool IsStreaming { get; set; }
        public int DurationMs { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual LlmModel Model { get; set; } = null!;
        public virtual LlmProviderCredential? Credential { get; set; }
    }
}
