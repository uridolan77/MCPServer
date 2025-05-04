using System;
using System.Collections.Generic;

namespace MCPServer.Core.Models.Responses
{
    public class ChatUsageStatsResponse
    {
        public int TotalMessages { get; set; }
        public int TotalTokensUsed { get; set; }
        public decimal TotalCost { get; set; }
        public List<ModelUsageStatResponse> ModelStats { get; set; } = new List<ModelUsageStatResponse>();
        public List<ProviderUsageStatResponse> ProviderStats { get; set; } = new List<ProviderUsageStatResponse>();
    }

    public class ModelUsageStatResponse
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int MessagesCount { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    public class ProviderUsageStatResponse
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int MessagesCount { get; set; }
        public int TotalTokens { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    public class ChatUsageLogResponse
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty; // Added message field
        public string Response { get; set; } = string.Empty; // Added response field
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal EstimatedCost { get; set; }
        public int DurationMs { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}