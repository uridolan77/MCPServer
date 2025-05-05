using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Features.Providers.Services.Interfaces
{
    public interface ILlmProviderService
    {
        // Provider management
        Task<List<LlmProvider>> GetAllProvidersAsync();
        Task<LlmProvider?> GetProviderByIdAsync(int id);
        Task<LlmProvider?> GetProviderByNameAsync(string name);
        Task<LlmProvider> AddProviderAsync(LlmProvider provider);
        Task<bool> UpdateProviderAsync(LlmProvider provider);
        Task<bool> DeleteProviderAsync(int id);

        // Model management
        Task<List<LlmModel>> GetAllModelsAsync();
        Task<List<LlmModel>> GetModelsByProviderIdAsync(int providerId);
        Task<LlmModel?> GetModelByIdAsync(int id);
        Task<LlmModel?> GetModelByProviderAndModelIdAsync(int providerId, string modelId);
        Task<LlmModel> AddModelAsync(LlmModel model);
        Task<bool> UpdateModelAsync(LlmModel model);
        Task<bool> DeleteModelAsync(int id);

        // Credential management
        Task<List<LlmProviderCredential>> GetCredentialsByProviderIdAsync(int providerId);
        Task<List<LlmProviderCredential>> GetCredentialsByUserIdAsync(Guid? userId);
        Task<LlmProviderCredential?> GetCredentialByIdAsync(int id);
        Task<LlmProviderCredential?> GetDefaultCredentialAsync(int providerId, Guid? userId = null);
        Task<LlmProviderCredential> AddCredentialAsync(LlmProviderCredential credential, string encryptionKey);
        Task<bool> UpdateCredentialAsync(LlmProviderCredential credential, string encryptionKey);
        Task<bool> DeleteCredentialAsync(int id);
        Task<bool> SetDefaultCredentialAsync(int credentialId, Guid? userId = null);

        // Usage logging
        Task LogUsageAsync(LlmUsageLog usageLog);
        Task<List<LlmUsageLog>> GetUsageLogsByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<LlmUsageLog>> GetUsageLogsByModelIdAsync(int modelId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalCostByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
