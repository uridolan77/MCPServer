using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository implementation for LlmProvider entities
    /// </summary>
    public class LlmProviderRepository : Repository<LlmProvider>, ILlmProviderRepository
    {
        public LlmProviderRepository(McpServerDbContext context, ILogger<LlmProviderRepository> logger)
            : base(context, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmProvider>> GetEnabledProvidersAsync()
        {
            try
            {
                return await _dbSet
                    .Where(p => p.IsEnabled)
                    .OrderBy(p => p.DisplayName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled providers");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmProvider?> GetProviderByNameAsync(string name)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(p => p.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by name: {Name}", name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmModel>> GetModelsForProviderAsync(int providerId)
        {
            try
            {
                return await _context.LlmModels
                    .Where(m => m.ProviderId == providerId)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models for provider: {ProviderId}", providerId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmModel>> GetEnabledModelsForProviderAsync(int providerId)
        {
            try
            {
                return await _context.LlmModels
                    .Where(m => m.ProviderId == providerId && m.IsEnabled)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled models for provider: {ProviderId}", providerId);
                throw;
            }
        }
    }
}
