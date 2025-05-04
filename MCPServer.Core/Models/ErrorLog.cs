using System;

namespace MCPServer.Core.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public Guid? UserId { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
