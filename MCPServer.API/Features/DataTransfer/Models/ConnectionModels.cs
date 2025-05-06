using System.ComponentModel.DataAnnotations;

namespace MCPServer.API.Features.DataTransfer.Models
{
    public class ConnectionRequest
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string ConnectionString { get; set; }
        
        public string Description { get; set; }
        
        public bool IsSource { get; set; }
    }

    public class ConnectionResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSource { get; set; }
        public string ConnectionStringMasked { get; set; }
    }

    public class ConnectionTestRequest
    {
        [Required]
        public string ConnectionString { get; set; }
    }

    public class ConnectionTestResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ServerInfo { get; set; }
        public string Version { get; set; }
    }
}