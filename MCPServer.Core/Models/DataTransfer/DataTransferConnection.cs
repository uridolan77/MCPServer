using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPServer.Core.Models.DataTransfer
{
    public enum ConnectionAccessLevel
    {
        ReadOnly,
        WriteOnly,
        ReadWrite
    }

    public class DataTransferConnection
    {
        public int ConnectionId { get; set; }
        public string? ConnectionName { get; set; }
        public string? ConnectionString { get; set; }
        public string? ConnectionAccessLevel { get; set; }
        public string? Description { get; set; }
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string AdditionalParameters { get; set; }
        public bool IsActive { get; set; }
        public bool? IsConnectionValid { get; set; }
        public int? MinPoolSize { get; set; }
        public int? MaxPoolSize { get; set; }
        public int? Timeout { get; set; }
        public bool? TrustServerCertificate { get; set; }
        public bool? Encrypt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public DateTime? LastTestedOn { get; set; }

        
        // For backward compatibility with code that uses the enum version
        [NotMapped]
        public ConnectionAccessLevel AccessLevel
        {
            get 
            {
                if (string.IsNullOrEmpty(ConnectionAccessLevel))
                {
                    return Core.Models.DataTransfer.ConnectionAccessLevel.ReadOnly;
                }
                
                return Enum.TryParse<ConnectionAccessLevel>(ConnectionAccessLevel, out var result) 
                    ? result 
                    : Core.Models.DataTransfer.ConnectionAccessLevel.ReadOnly;
            }
            set
            {
                ConnectionAccessLevel = value.ToString();
            }
        }
    }
}