using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository interface for ChatUsageLog entities
    /// </summary>
    public interface IChatUsageLogRepository : IRepository<ChatUsageLog>
    {
        /// <summary>
        /// Gets usage logs by user ID
        /// </summary>
        Task<IEnumerable<ChatUsageLog>> GetUsageLogsByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets usage logs by model ID
        /// </summary>
        Task<IEnumerable<ChatUsageLog>> GetUsageLogsByModelIdAsync(int modelId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets usage logs by provider ID
        /// </summary>
        Task<IEnumerable<ChatUsageLog>> GetUsageLogsByProviderIdAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets usage logs by session ID
        /// </summary>
        Task<IEnumerable<ChatUsageLog>> GetUsageLogsBySessionIdAsync(string sessionId);

        /// <summary>
        /// Gets total cost by user ID
        /// </summary>
        Task<decimal> GetTotalCostByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}

