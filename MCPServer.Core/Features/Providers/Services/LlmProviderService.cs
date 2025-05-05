using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Data.Repositories;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IUnitOfWork = MCPServer.Core.Features.Shared.Services.Interfaces.IUnitOfWork;
using ICachingService = MCPServer.Core.Features.Shared.Services.Interfaces.ICachingService;
using MCPServer.Core.Features.Providers.Services.Interfaces;

namespace MCPServer.Core.Features.Providers.Services
{
    public class LlmProviderService : ILlmProviderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILlmProviderRepository _providerRepository;
        private readonly ILlmModelRepository _modelRepository;
        private readonly ICachingService _cachingService;
        private readonly ILogger<LlmProviderService> _logger;

        public LlmProviderService(
            IUnitOfWork unitOfWork,
            ILlmProviderRepository providerRepository,
            ILlmModelRepository modelRepository,
            ICachingService cachingService,
            ILogger<LlmProviderService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Provider Management

        public async Task<List<LlmProvider>> GetAllProvidersAsync()
        {
            return await _cachingService.GetOrCreateAsync(
                "providers:all",
                async () =>
                {
                    _logger.LogDebug("Cache miss for all providers, fetching from database");
                    var providers = await _providerRepository.GetAllAsync();
                    return providers.ToList();
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task<LlmProvider?> GetProviderByIdAsync(int id)
        {
            return await _cachingService.GetOrCreateAsync(
                $"provider:{id}",
                async () =>
                {
                    _logger.LogDebug("Cache miss for provider ID {ProviderId}, fetching from database", id);
                    return await _providerRepository.GetByIdAsync(id);
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task<LlmProvider?> GetProviderByNameAsync(string name)
        {
            return await _providerRepository.GetProviderByNameAsync(name);
        }

        public async Task<LlmProvider> AddProviderAsync(LlmProvider provider)
        {
            provider.CreatedAt = DateTime.UtcNow;
            await _providerRepository.AddAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added LLM provider: {ProviderName} (ID: {ProviderId})",
                provider.Name, provider.Id);

            // Invalidate cache
            _cachingService.Remove("providers:all");

            return provider;
        }

        public async Task<bool> UpdateProviderAsync(LlmProvider provider)
        {
            var existingProvider = await _providerRepository.GetByIdAsync(provider.Id);

            if (existingProvider == null)
            {
                _logger.LogWarning("Cannot update provider. Provider not found with ID: {ProviderId}", provider.Id);
                return false;
            }

            provider.UpdatedAt = DateTime.UtcNow;
            await _providerRepository.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated LLM provider: {ProviderName} (ID: {ProviderId})",
                provider.Name, provider.Id);

            // Invalidate cache
            _cachingService.Remove("providers:all");
            _cachingService.Remove($"provider:{provider.Id}");

            return true;
        }

        public async Task<bool> DeleteProviderAsync(int id)
        {
            var provider = await _providerRepository.GetByIdAsync(id);

            if (provider == null)
            {
                _logger.LogWarning("Cannot delete provider. Provider not found with ID: {ProviderId}", id);
                return false;
            }

            await _providerRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted LLM provider: {ProviderName} (ID: {ProviderId})",
                provider.Name, provider.Id);

            // Invalidate cache
            _cachingService.Remove("providers:all");
            _cachingService.Remove($"provider:{id}");
            _cachingService.Remove($"models:provider:{id}");

            return true;
        }

        #endregion

        #region Model Management

        public async Task<List<LlmModel>> GetAllModelsAsync()
        {
            return await _cachingService.GetOrCreateAsync(
                "models:all",
                async () =>
                {
                    _logger.LogDebug("Cache miss for all models, fetching from database");
                    var models = await _modelRepository.GetAllModelsWithProvidersAsync();
                    return models.ToList();
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task<List<LlmModel>> GetModelsByProviderIdAsync(int providerId)
        {
            return await _cachingService.GetOrCreateAsync(
                $"models:provider:{providerId}",
                async () =>
                {
                    _logger.LogDebug("Cache miss for models by provider ID {ProviderId}, fetching from database", providerId);
                    var models = await _providerRepository.GetModelsForProviderAsync(providerId);
                    return models.ToList();
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task<LlmModel?> GetModelByIdAsync(int id)
        {
            return await _cachingService.GetOrCreateAsync(
                $"model:{id}",
                async () =>
                {
                    _logger.LogDebug("Cache miss for model ID {ModelId}, fetching from database", id);
                    return await _modelRepository.GetModelWithProviderAsync(id);
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task<LlmModel?> GetModelByProviderAndModelIdAsync(int providerId, string modelId)
        {
            return await _modelRepository.FindSingleAsync(m => m.ProviderId == providerId && m.ModelId == modelId);
        }

        public async Task<LlmModel> AddModelAsync(LlmModel model)
        {
            model.CreatedAt = DateTime.UtcNow;
            await _modelRepository.AddAsync(model);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added LLM model: {ModelName} (ID: {ModelId}) for provider {ProviderId}",
                model.Name, model.Id, model.ProviderId);

            // Invalidate cache
            _cachingService.Remove("models:all");
            _cachingService.Remove($"models:provider:{model.ProviderId}");

            return model;
        }

        public async Task<bool> UpdateModelAsync(LlmModel model)
        {
            var existingModel = await _modelRepository.GetByIdAsync(model.Id);

            if (existingModel == null)
            {
                _logger.LogWarning("Cannot update model. Model not found with ID: {ModelId}", model.Id);
                return false;
            }

            model.UpdatedAt = DateTime.UtcNow;
            await _modelRepository.UpdateAsync(model);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated LLM model: {ModelName} (ID: {ModelId})",
                model.Name, model.Id);

            // Invalidate cache
            _cachingService.Remove("models:all");
            _cachingService.Remove($"model:{model.Id}");
            _cachingService.Remove($"models:provider:{model.ProviderId}");

            // If provider ID changed, invalidate old provider's models cache
            if (existingModel.ProviderId != model.ProviderId)
            {
                _cachingService.Remove($"models:provider:{existingModel.ProviderId}");
            }

            return true;
        }

        public async Task<bool> DeleteModelAsync(int id)
        {
            var model = await _modelRepository.GetByIdAsync(id);

            if (model == null)
            {
                _logger.LogWarning("Cannot delete model. Model not found with ID: {ModelId}", id);
                return false;
            }

            await _modelRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted LLM model: {ModelName} (ID: {ModelId})",
                model.Name, model.Id);

            // Invalidate cache
            _cachingService.Remove("models:all");
            _cachingService.Remove($"model:{id}");
            _cachingService.Remove($"models:provider:{model.ProviderId}");

            return true;
        }

        #endregion

        #region Credential Management

        public async Task<List<LlmProviderCredential>> GetCredentialsByProviderIdAsync(int providerId)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            var credentials = await credentialRepository.FindAsync(c => c.ProviderId == providerId);
            return credentials.ToList();
        }

        public async Task<List<LlmProviderCredential>> GetCredentialsByUserIdAsync(Guid? userId)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            var credentials = await credentialRepository.FindAsync(c => c.UserId == userId || c.UserId == null);
            return credentials.ToList();
        }

        public async Task<LlmProviderCredential?> GetCredentialByIdAsync(int id)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            return await credentialRepository.GetByIdAsync(id);
        }

        public async Task<LlmProviderCredential?> GetDefaultCredentialAsync(int providerId, Guid? userId = null)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();

            // First try to get user-specific default credential
            if (userId.HasValue)
            {
                var userCredential = await credentialRepository.FindSingleAsync(c =>
                    c.ProviderId == providerId &&
                    c.UserId == userId &&
                    c.IsDefault &&
                    c.IsEnabled);

                if (userCredential != null)
                {
                    return userCredential;
                }
            }

            // Fall back to system-wide default credential
            return await credentialRepository.FindSingleAsync(c =>
                c.ProviderId == providerId &&
                c.UserId == null &&
                c.IsDefault &&
                c.IsEnabled);
        }

        public async Task<LlmProviderCredential> AddCredentialAsync(LlmProviderCredential credential, string encryptionKey)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();

            // If this is the first credential for this provider/user, make it the default
            var existingCredentials = await credentialRepository.FindAsync(c =>
                c.ProviderId == credential.ProviderId &&
                (c.UserId == credential.UserId || (c.UserId == null && credential.UserId == null)));

            if (!existingCredentials.Any())
            {
                credential.IsDefault = true;
            }

            credential.CreatedAt = DateTime.UtcNow;
            await credentialRepository.AddAsync(credential);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added credential: {CredentialName} (ID: {CredentialId}) for provider {ProviderId}",
                credential.Name, credential.Id, credential.ProviderId);

            return credential;
        }

        public async Task<bool> UpdateCredentialAsync(LlmProviderCredential credential, string encryptionKey)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            var existingCredential = await credentialRepository.GetByIdAsync(credential.Id);

            if (existingCredential == null)
            {
                _logger.LogWarning("Cannot update credential. Credential not found with ID: {CredentialId}", credential.Id);
                return false;
            }

            credential.UpdatedAt = DateTime.UtcNow;
            await credentialRepository.UpdateAsync(credential);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated credential: {CredentialName} (ID: {CredentialId})",
                credential.Name, credential.Id);

            return true;
        }

        public async Task<bool> DeleteCredentialAsync(int id)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            var credential = await credentialRepository.GetByIdAsync(id);

            if (credential == null)
            {
                _logger.LogWarning("Cannot delete credential. Credential not found with ID: {CredentialId}", id);
                return false;
            }

            await credentialRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted credential: {CredentialName} (ID: {CredentialId})",
                credential.Name, credential.Id);

            return true;
        }

        public async Task<bool> SetDefaultCredentialAsync(int credentialId, Guid? userId = null)
        {
            var credentialRepository = _unitOfWork.GetRepository<LlmProviderCredential>();
            var credential = await credentialRepository.GetByIdAsync(credentialId);

            if (credential == null)
            {
                _logger.LogWarning("Cannot set default credential. Credential not found with ID: {CredentialId}", credentialId);
                return false;
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Clear default flag for all credentials for this provider and user
                var existingCredentials = await credentialRepository.FindAsync(c =>
                    c.ProviderId == credential.ProviderId &&
                    (c.UserId == credential.UserId || (c.UserId == null && credential.UserId == null)));

                foreach (var existingCredential in existingCredentials)
                {
                    existingCredential.IsDefault = false;
                    await credentialRepository.UpdateAsync(existingCredential);
                }

                // Set this credential as default
                credential.IsDefault = true;
                await credentialRepository.UpdateAsync(credential);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Set credential {CredentialName} (ID: {CredentialId}) as default for provider {ProviderId}",
                    credential.Name, credential.Id, credential.ProviderId);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to set default credential");
                return false;
            }
        }

        #endregion

        #region Usage Logging

        public async Task LogUsageAsync(LlmUsageLog usageLog)
        {
            var usageLogRepository = _unitOfWork.GetRepository<LlmUsageLog>();
            usageLog.Timestamp = DateTime.UtcNow;
            await usageLogRepository.AddAsync(usageLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Logged LLM usage: Model {ModelId}, User {UserId}, Tokens {InputTokens}/{OutputTokens}, Cost {Cost}",
                usageLog.ModelId, usageLog.UserId, usageLog.InputTokens, usageLog.OutputTokens, usageLog.EstimatedCost);
        }

        public async Task<List<LlmUsageLog>> GetUsageLogsByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var usageLogRepository = _unitOfWork.GetRepository<LlmUsageLog>();
            var query = usageLogRepository.Query();

            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public async Task<List<LlmUsageLog>> GetUsageLogsByProviderIdAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var usageLogRepository = _unitOfWork.GetRepository<LlmUsageLog>();

            // Join with the Model table to get the provider ID
            var query = usageLogRepository.Query()
                .Include(l => l.Model)
                .Where(l => l.Model.ProviderId == providerId);

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public async Task<List<LlmUsageLog>> GetUsageLogsByModelIdAsync(int modelId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var usageLogRepository = _unitOfWork.GetRepository<LlmUsageLog>();
            var query = usageLogRepository.Query().Where(l => l.ModelId == modelId);

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }



        public async Task<decimal> GetTotalCostByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var usageLogRepository = _unitOfWork.GetRepository<LlmUsageLog>();
            var query = usageLogRepository.Query().Where(l => l.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            return await query.SumAsync(l => l.EstimatedCost);
        }

        #endregion
    }
}




