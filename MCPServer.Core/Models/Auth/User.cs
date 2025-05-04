using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MCPServer.Core.Models.Auth
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public List<string> Roles { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Navigation property for owned sessions
        public List<string> OwnedSessionIds { get; set; } = new List<string>();
    }
}
