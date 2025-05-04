using System;

namespace MCPServer.Core.Models
{
    public class McpResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public bool IsComplete { get; set; } = true;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
