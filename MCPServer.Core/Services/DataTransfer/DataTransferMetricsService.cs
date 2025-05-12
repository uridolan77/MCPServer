using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Models.DataTransfer;

namespace MCPServer.Core.Services.DataTransfer
{
    public interface IDataTransferMetricsService
    {
        Task<List<DataTransferRun>> GetRunHistoryAsync(int configurationId = 0, int limit = 50);
        Task<DataTransferRun> GetRunByIdAsync(int runId);
        Task<List<DataTransferTableMetric>> GetRunMetricsAsync(int runId);
    }

    public class DataTransferMetricsService : IDataTransferMetricsService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<DataTransferMetricsService> _logger;

        public DataTransferMetricsService(DbContext dbContext, ILogger<DataTransferMetricsService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<DataTransferRun>> GetRunHistoryAsync(int configurationId = 0, int limit = 50)
        {
            try
            {
                var query = _dbContext.Set<DataTransferRun>()
                    .Include(r => r.Configuration)
                    .AsQueryable();

                if (configurationId > 0)
                {
                    query = query.Where(r => r.ConfigurationId == configurationId);
                }

                return await query
                    .OrderByDescending(r => r.StartTime)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run history data. ConfigurationId: {ConfigId}, Limit: {Limit}", 
                    configurationId, limit);
                throw;
            }
        }

        public async Task<DataTransferRun> GetRunByIdAsync(int runId)
        {
            try
            {
                return await _dbContext.Set<DataTransferRun>()
                    .Include(r => r.Configuration)
                    .FirstOrDefaultAsync(r => r.RunId == runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving run details for RunId: {RunId}", runId);
                throw;
            }
        }

        public async Task<List<DataTransferTableMetric>> GetRunMetricsAsync(int runId)
        {
            try
            {
                return await _dbContext.Set<DataTransferTableMetric>()
                    .Where(m => m.RunId == runId)
                    .OrderBy(m => m.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics for RunId: {RunId}", runId);
                throw;
            }
        }
    }
}