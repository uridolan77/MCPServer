using System;
using System.ComponentModel.DataAnnotations;

namespace MCPServer.API.Features.DataTransfer.Models
{
    public class ConnectionRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsSource { get; set; }
        
        public bool IsDestination { get; set; }
    }

    public class ConnectionResponse
    {
        public int ConnectionId { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ConnectionAccessLevel { get; set; } = string.Empty;
        public string ConnectionStringMasked { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty; // Added for edit functionality
        public DateTime? LastTestedOn { get; set; }
        public string Source { get; set; } = "Local"; 
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? AdditionalParameters { get; set; }
        public bool IsActive { get; set; }
        public bool? IsConnectionValid { get; set; } // <--- ADDED
        public int? MinPoolSize { get; set; }
        public int? MaxPoolSize { get; set; }
        public int? Timeout { get; set; }
        public bool? TrustServerCertificate { get; set; }
        public bool? Encrypt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
    }

    public class ConnectionTestRequest
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class ConnectionTestResponse
    {
        public bool Success { get; set; }
        public bool IsSuccess { get => Success; set => Success = value; }
        public string Message { get; set; } = string.Empty;
        public string ServerInfo { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool? IsConnectionValid { get; set; } // <--- ADDED
    }

    public class ConnectionUpdateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsSource { get; set; }
        public bool? IsDestination { get; set; }
        public bool? IsActive { get; set; }
        public string? ConnectionString { get; set; }
        // Fields that, if changed, should invalidate IsConnectionValid
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? AdditionalParameters { get; set; }
        public int? MinPoolSize { get; set; }
        public int? MaxPoolSize { get; set; }
        public int? Timeout { get; set; }
        public bool? TrustServerCertificate { get; set; }
        public bool? Encrypt { get; set; }
    }
}