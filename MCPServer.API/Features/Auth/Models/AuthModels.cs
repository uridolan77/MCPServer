using System;

namespace MCPServer.API.Features.Auth.Models
{
    /// <summary>
    /// Request model for credential operations
    /// </summary>
    public class CredentialRequest
    {
        /// <summary>
        /// The provider ID
        /// </summary>
        public int ProviderId { get; set; }

        /// <summary>
        /// The user ID (null for system-wide credentials)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// The credential name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The credential data
        /// </summary>
        public object? Credentials { get; set; }
    }

    /// <summary>
    /// Request model for refreshing tokens
    /// </summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
