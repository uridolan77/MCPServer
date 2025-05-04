using System;

namespace MCPServer.Core.Models
{
    public class Session
    {
        public string SessionId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}