using System;
using System.Collections.Generic;

namespace MCPServer.Core.Models
{
    public class SessionDataDto
    {
        public List<Message> Messages { get; set; } = new List<Message>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}