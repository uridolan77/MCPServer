using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Features.Shared.Services;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository implementation for ChatUsageLog entities
    /// </summary>
    public class ChatUsageLogRepository : Repository<ChatUsageLog>, IChatUsageLogRepository
    {
        public ChatUsageLogRepository(McpServerDbContext context, ILogger<ChatUsageLogRepository> logger)
            : base(context, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ChatUsageLog>> GetUsageLogsByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbSet.AsQueryable().Where(l => l.UserId == userId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage logs by user ID: {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ChatUsageLog>> GetUsageLogsByModelIdAsync(int modelId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbSet.AsQueryable().Where(l => l.ModelId == modelId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage logs by model ID: {ModelId}", modelId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ChatUsageLog>> GetUsageLogsByProviderIdAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbSet.AsQueryable().Where(l => l.ProviderId == providerId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage logs by provider ID: {ProviderId}", providerId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ChatUsageLog>> GetUsageLogsBySessionIdAsync(string sessionId)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.SessionId == sessionId)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage logs by session ID: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<decimal> GetTotalCostByUserIdAsync(Guid? userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbSet.AsQueryable().Where(l => l.UserId == userId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total cost by user ID: {UserId}", userId);
                throw;
            }
        }
    }
}

