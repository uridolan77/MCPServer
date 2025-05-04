using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCPServer.Core.Models.Llm
{
    public class LlmProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // Added for UI display purposes
        public string ApiEndpoint { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string AuthType { get; set; } = "ApiKey"; // ApiKey, OAuth, etc.
        public string ConfigSchema { get; set; } = "{}"; // JSON schema for provider-specific configuration
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<LlmModel> Models { get; set; } = new List<LlmModel>();
        public virtual ICollection<LlmProviderCredential> Credentials { get; set; } = new List<LlmProviderCredential>();
    }
}
