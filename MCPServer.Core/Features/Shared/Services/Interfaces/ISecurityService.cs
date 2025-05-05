using System.Threading.Tasks;

namespace MCPServer.Core.Features.Shared.Services.Interfaces
{
    /// <summary>
    /// Interface for security operations like encryption and decryption
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Encrypts the provided plain text
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>The encrypted text</returns>
        Task<string> EncryptAsync(string plainText);
        
        /// <summary>
        /// Decrypts the provided encrypted text
        /// </summary>
        /// <param name="encryptedText">The text to decrypt</param>
        /// <returns>The decrypted text</returns>
        Task<string> DecryptAsync(string encryptedText);
        
        /// <summary>
        /// Gets the encryption key from the configuration
        /// </summary>
        /// <returns>The encryption key</returns>
        Task<string> GetEncryptionKeyAsync();
    }
}
