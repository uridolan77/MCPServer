using System;
using System.ComponentModel.DataAnnotations;

namespace MCPServer.Core.Models.Auth
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool Revoked { get; set; }

        public DateTime? RevokedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
