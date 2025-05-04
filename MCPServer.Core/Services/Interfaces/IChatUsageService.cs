using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Responses;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IChatUsageService
    {
        Task LogChatUsageAsync(
            string sessionId, 
            string message, 
            string response, 
            LlmModel? model, 
            int duration, 
            bool success, 
            string? errorMessage = null, 
            List<Message>? sessionHistory = null,
            string? username = null);
            
        Task<ChatUsageStatsResponse> GetOverallStatsAsync();
        
        Task<List<ChatUsageLogResponse>> GetFilteredLogsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? modelId,
            int? providerId,
            string? sessionId,
            int page = 1,
            int pageSize = 20);
            
        Task<ModelUsageStatResponse?> GetModelStatsAsync(int modelId);
        
        Task<ProviderUsageStatResponse?> GetProviderStatsAsync(int providerId);
        
#if DEBUG
        Task<object> GetDiagnosticInfoAsync();
        
        Task<object> GenerateSampleDataAsync(int count = 10);
#endif
    }
}