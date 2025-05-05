using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Features.Shared.Services
{
    /// <summary>
    /// Implementation of security service for encryption and decryption
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(
            IConfiguration configuration,
            ILogger<SecurityService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> EncryptAsync(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    return string.Empty;
                }

                var key = await GetEncryptionKeyAsync();
                
                // Use AES encryption
                using var aes = Aes.Create();
                aes.Key = DeriveKeyFromString(key, aes.KeySize / 8);
                aes.IV = new byte[aes.BlockSize / 8]; // Use zero IV for simplicity, in production use a random IV
                
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    await sw.WriteAsync(plainText);
                }
                
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> DecryptAsync(string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                {
                    return string.Empty;
                }

                var key = await GetEncryptionKeyAsync();
                
                // Use AES decryption
                using var aes = Aes.Create();
                aes.Key = DeriveKeyFromString(key, aes.KeySize / 8);
                aes.IV = new byte[aes.BlockSize / 8]; // Use zero IV for simplicity, in production use the IV from encryption
                
                var cipherBytes = Convert.FromBase64String(encryptedText);
                
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                
                return await sr.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw;
            }
        }

        /// <inheritdoc />
        public Task<string> GetEncryptionKeyAsync()
        {
            var key = _configuration["AppSettings:EncryptionKey"];
            
            if (string.IsNullOrEmpty(key))
            {
                // Fallback to development key if not configured
                // In production, this should throw an exception
                if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
                {
                    key = "dev-encryption-key-minimum-length-32-chars";
                    _logger.LogWarning("Using development encryption key. This should not be used in production.");
                }
                else
                {
                    throw new InvalidOperationException("Encryption key is not configured for production");
                }
            }
            
            return Task.FromResult(key);
        }
        
        /// <summary>
        /// Derives a key of the specified length from the provided string
        /// </summary>
        private byte[] DeriveKeyFromString(string key, int keyLength)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                key,
                Encoding.UTF8.GetBytes("MCPServerSalt"), // Use a fixed salt for simplicity
                10000, // Number of iterations
                HashAlgorithmName.SHA256);
                
            return deriveBytes.GetBytes(keyLength);
        }
    }
}



