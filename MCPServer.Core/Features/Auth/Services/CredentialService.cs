using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IUnitOfWork = MCPServer.Core.Features.Shared.Services.Interfaces.IUnitOfWork;
using ISecurityService = MCPServer.Core.Features.Shared.Services.Interfaces.ISecurityService;
using MCPServer.Core.Features.Auth.Services.Interfaces;

namespace MCPServer.Core.Features.Auth.Services
{
    /// <summary>
    /// Service for managing LLM provider credentials
    /// </summary>
    public class CredentialService : ICredentialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISecurityService _securityService;
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(
            IUnitOfWork unitOfWork,
            ISecurityService securityService,
            ILogger<CredentialService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<List<LlmProviderCredential>> GetCredentialsByProviderIdAsync(int providerId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credentials = await repository.Query()
                    .Include(c => c.Provider)
                    .Where(c => c.ProviderId == providerId)
                    .ToListAsync();

                // Redact sensitive information
                foreach (var credential in credentials)
                {
                    credential.EncryptedCredentials = "[REDACTED]";
                    credential.ApiKey = "[REDACTED]";
                }

                return credentials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials for provider {ProviderId}", providerId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<LlmProviderCredential>> GetCredentialsByUserIdAsync(Guid? userId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credentials = await repository.Query()
                    .Include(c => c.Provider)
                    .Where(c => c.UserId == userId || c.UserId == null)
                    .ToListAsync();

                // Redact sensitive information
                foreach (var credential in credentials)
                {
                    credential.EncryptedCredentials = "[REDACTED]";
                    credential.ApiKey = "[REDACTED]";
                }

                return credentials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmProviderCredential?> GetCredentialByIdAsync(int id)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credential = await repository.Query()
                    .Include(c => c.Provider)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (credential != null)
                {
                    // Redact sensitive information
                    credential.EncryptedCredentials = "[REDACTED]";
                    credential.ApiKey = "[REDACTED]";
                }

                return credential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential with ID {CredentialId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmProviderCredential?> GetDefaultCredentialAsync(int providerId, Guid? userId = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                // First try to get user-specific default credential
                if (userId.HasValue)
                {
                    var userCredential = await repository.Query()
                        .Include(c => c.Provider)
                        .FirstOrDefaultAsync(c => c.ProviderId == providerId &&
                                                c.UserId == userId &&
                                                c.IsDefault &&
                                                c.IsEnabled);

                    if (userCredential != null)
                    {
                        // Redact sensitive information
                        userCredential.EncryptedCredentials = "[REDACTED]";
                        userCredential.ApiKey = "[REDACTED]";
                        return userCredential;
                    }
                }

                // Fall back to system-wide default credential
                var systemCredential = await repository.Query()
                    .Include(c => c.Provider)
                    .FirstOrDefaultAsync(c => c.ProviderId == providerId &&
                                            c.UserId == null &&
                                            c.IsDefault &&
                                            c.IsEnabled);

                if (systemCredential != null)
                {
                    // Redact sensitive information
                    systemCredential.EncryptedCredentials = "[REDACTED]";
                    systemCredential.ApiKey = "[REDACTED]";
                }

                return systemCredential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default credential for provider {ProviderId} and user {UserId}", providerId, userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmProviderCredential> AddCredentialAsync(LlmProviderCredential credential, object rawCredentials)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                // Check if this is the first credential for this provider/user
                var existingCredentials = await repository.Query()
                    .Where(c => c.ProviderId == credential.ProviderId &&
                              (c.UserId == credential.UserId || (c.UserId == null && credential.UserId == null)))
                    .ToListAsync();

                if (existingCredentials.Count == 0)
                {
                    credential.IsDefault = true;
                }

                // Encrypt the credentials
                var json = JsonSerializer.Serialize(rawCredentials);
                credential.EncryptedCredentials = await _securityService.EncryptAsync(json);

                // Set creation timestamp
                credential.CreatedAt = DateTime.UtcNow;
                credential.UpdatedAt = DateTime.UtcNow;

                // Add the credential
                await repository.AddAsync(credential);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Added credential: {CredentialName} (ID: {CredentialId}) for provider {ProviderId}",
                    credential.Name, credential.Id, credential.ProviderId);

                // Redact sensitive information before returning
                var result = await GetCredentialByIdAsync(credential.Id);
                return result ?? credential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding credential for provider {ProviderId}", credential.ProviderId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateCredentialAsync(LlmProviderCredential credential, object? rawCredentials = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var existingCredential = await repository.GetByIdAsync(credential.Id);

                if (existingCredential == null)
                {
                    _logger.LogWarning("Cannot update credential. Credential not found with ID: {CredentialId}", credential.Id);
                    return false;
                }

                // Update the credential properties
                existingCredential.Name = credential.Name;
                existingCredential.ProviderId = credential.ProviderId;
                existingCredential.IsEnabled = credential.IsEnabled;
                existingCredential.UpdatedAt = DateTime.UtcNow;

                // Update the encrypted credentials if provided
                if (rawCredentials != null)
                {
                    var json = JsonSerializer.Serialize(rawCredentials);
                    existingCredential.EncryptedCredentials = await _securityService.EncryptAsync(json);
                }

                await repository.UpdateAsync(existingCredential);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated credential: {CredentialName} (ID: {CredentialId})",
                    credential.Name, credential.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credential with ID {CredentialId}", credential.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteCredentialAsync(int id)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credential = await repository.GetByIdAsync(id);

                if (credential == null)
                {
                    _logger.LogWarning("Cannot delete credential. Credential not found with ID: {CredentialId}", id);
                    return false;
                }

                await repository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted credential with ID: {CredentialId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credential with ID {CredentialId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetDefaultCredentialAsync(int credentialId, Guid? userId = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credential = await repository.GetByIdAsync(credentialId);

                if (credential == null)
                {
                    _logger.LogWarning("Cannot set default credential. Credential not found with ID: {CredentialId}", credentialId);
                    return false;
                }

                // Begin transaction
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Clear default flag for all credentials for this provider and user
                    var existingCredentials = await repository.Query()
                        .Where(c => c.ProviderId == credential.ProviderId &&
                                  (c.UserId == credential.UserId || (c.UserId == null && credential.UserId == null)))
                        .ToListAsync();

                    foreach (var existingCredential in existingCredentials)
                    {
                        existingCredential.IsDefault = false;
                        await repository.UpdateAsync(existingCredential);
                    }

                    // Set this credential as default
                    credential.IsDefault = true;
                    await repository.UpdateAsync(credential);

                    // Commit transaction
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("Set credential {CredentialName} (ID: {CredentialId}) as default for provider {ProviderId}",
                        credential.Name, credential.Id, credential.ProviderId);

                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error setting default credential with ID {CredentialId}", credentialId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default credential with ID {CredentialId}", credentialId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<T?> GetDecryptedCredentialsAsync<T>(int credentialId) where T : class
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credential = await repository.GetByIdAsync(credentialId);

                if (credential == null || string.IsNullOrEmpty(credential.EncryptedCredentials))
                {
                    return null;
                }

                // Decrypt the credentials
                var json = await _securityService.DecryptAsync(credential.EncryptedCredentials);

                // Update last used timestamp
                credential.LastUsedAt = DateTime.UtcNow;
                await repository.UpdateAsync(credential);
                await _unitOfWork.SaveChangesAsync();

                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting decrypted credentials for credential with ID {CredentialId}", credentialId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateUserAccessAsync(int credentialId, Guid? userId, bool requireAdmin = false)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<LlmProviderCredential>();

                var credential = await repository.GetByIdAsync(credentialId);

                if (credential == null)
                {
                    return false;
                }

                // If admin is required, the caller should check the user's roles
                if (requireAdmin)
                {
                    return true; // The caller will check admin role
                }

                // Check if the credential belongs to the user or is a system credential
                return credential.UserId == null || credential.UserId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user access for credential with ID {CredentialId}", credentialId);
                throw;
            }
        }
    }
}




