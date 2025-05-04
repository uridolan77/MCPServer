using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for managing LLM provider credentials
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Gets all credentials for a provider
        /// </summary>
        Task<List<LlmProviderCredential>> GetCredentialsByProviderIdAsync(int providerId);
        
        /// <summary>
        /// Gets all credentials for a user
        /// </summary>
        Task<List<LlmProviderCredential>> GetCredentialsByUserIdAsync(Guid? userId);
        
        /// <summary>
        /// Gets a credential by its ID
        /// </summary>
        Task<LlmProviderCredential?> GetCredentialByIdAsync(int id);
        
        /// <summary>
        /// Gets the default credential for a provider and user
        /// </summary>
        Task<LlmProviderCredential?> GetDefaultCredentialAsync(int providerId, Guid? userId = null);
        
        /// <summary>
        /// Adds a new credential
        /// </summary>
        Task<LlmProviderCredential> AddCredentialAsync(LlmProviderCredential credential, object rawCredentials);
        
        /// <summary>
        /// Updates an existing credential
        /// </summary>
        Task<bool> UpdateCredentialAsync(LlmProviderCredential credential, object? rawCredentials = null);
        
        /// <summary>
        /// Deletes a credential
        /// </summary>
        Task<bool> DeleteCredentialAsync(int id);
        
        /// <summary>
        /// Sets a credential as the default for a provider and user
        /// </summary>
        Task<bool> SetDefaultCredentialAsync(int credentialId, Guid? userId = null);
        
        /// <summary>
        /// Gets the decrypted credentials for a provider
        /// </summary>
        Task<T?> GetDecryptedCredentialsAsync<T>(int credentialId) where T : class;
        
        /// <summary>
        /// Validates if a user has access to a credential
        /// </summary>
        Task<bool> ValidateUserAccessAsync(int credentialId, Guid? userId, bool requireAdmin = false);
    }
}
