using System;

namespace MCPServer.Core.Models
{
    public class UsageMetric
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public string MetricType { get; set; } = string.Empty; // e.g., "TokensUsed", "ApiCalls", "RagQueries"
        public long Value { get; set; }
        public string? SessionId { get; set; }
        public string? AdditionalData { get; set; } // JSON serialized additional data
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
