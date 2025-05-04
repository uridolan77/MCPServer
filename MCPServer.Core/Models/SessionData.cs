using System;

namespace MCPServer.Core.Models
{
    public class SessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string Data { get; set; } = string.Empty; // JSON serialized SessionContext
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}
