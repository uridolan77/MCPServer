using System;
using System.Collections.Generic;

namespace MCPServer.Core.Models
{
    public class SessionContext
    {
        public string SessionId { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new List<Message>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public int TotalTokens { get; set; } = 0;
    }
}
