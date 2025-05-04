using System;

namespace MCPServer.Core.Models
{
    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int TokenCount { get; set; } = 0;
    }
}
