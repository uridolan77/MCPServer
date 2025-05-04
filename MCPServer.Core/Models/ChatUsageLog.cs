using System;

namespace MCPServer.Core.Models
{
    public class ChatUsageLog
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public int? ModelId { get; set; }
        public string? ModelName { get; set; }
        public int? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
        public decimal EstimatedCost { get; set; }
        public int Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}