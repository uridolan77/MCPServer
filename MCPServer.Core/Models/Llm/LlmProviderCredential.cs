using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCPServer.Core.Models.Llm
{
    public class LlmProviderCredential
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public Guid? UserId { get; set; } // If null, it's a system-wide credential
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty; // Direct API Key storage for simple cases
        public string EncryptedCredentials { get; set; } = string.Empty; // Encrypted JSON with credentials
        public bool IsDefault { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }

        // Navigation property
        [JsonIgnore] // Break circular reference
        public virtual LlmProvider Provider { get; set; } = null!;

        // Helper methods for credential management
        public void SetCredentials(object credentials, string encryptionKey)
        {
            var json = JsonSerializer.Serialize(credentials);
            EncryptedCredentials = Encrypt(json, encryptionKey);
        }

        public T? GetCredentials<T>(string encryptionKey) where T : class
        {
            if (string.IsNullOrEmpty(EncryptedCredentials))
                return null;

            var json = Decrypt(EncryptedCredentials, encryptionKey);
            return JsonSerializer.Deserialize<T>(json);
        }

        // Simple encryption/decryption methods (in production, use a more secure approach)
        private string Encrypt(string text, string key)
        {
            // This is a placeholder. In a real application, use a proper encryption library
            // such as Azure Key Vault, AWS KMS, or a library like ProtectedData
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }

        private string Decrypt(string encryptedText, string key)
        {
            // This is a placeholder. In a real application, use a proper decryption library
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
        }
    }
}
