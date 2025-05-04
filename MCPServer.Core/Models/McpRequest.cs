using System.Collections.Generic;

namespace MCPServer.Core.Models
{
    public class McpRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
        public bool Stream { get; set; } = false;
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
