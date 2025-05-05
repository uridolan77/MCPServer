using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Features.Shared.Services;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository implementation for LlmModel entities
    /// </summary>
    public class LlmModelRepository : Repository<LlmModel>, ILlmModelRepository
    {
        public LlmModelRepository(McpServerDbContext context, ILogger<LlmModelRepository> logger)
            : base(context, logger)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmModel>> GetEnabledModelsAsync()
        {
            try
            {
                return await _dbSet
                    .Where(m => m.IsEnabled)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled models");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmModel?> GetModelByProviderModelIdAsync(string modelId)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(m => m.ModelId == modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model by provider model ID: {ModelId}", modelId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LlmModel?> GetModelWithProviderAsync(int id)
        {
            try
            {
                return await _dbSet
                    .Include(m => m.Provider)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model with provider: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmModel>> GetAllModelsWithProvidersAsync()
        {
            try
            {
                return await _dbSet
                    .Include(m => m.Provider)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all models with providers");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LlmModel>> GetEnabledModelsWithProvidersAsync()
        {
            try
            {
                return await _dbSet
                    .Include(m => m.Provider)
                    .Where(m => m.IsEnabled && m.Provider != null && m.Provider.IsEnabled)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled models with providers");
                throw;
            }
        }
    }
}

