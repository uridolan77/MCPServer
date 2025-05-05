using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/chat-usage")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class ChatUsageStatsController : ControllerBase
    {
        private readonly ILogger<ChatUsageStatsController> _logger;
        private readonly McpServerDbContext _dbContext;
        private readonly IChatUsageService _chatUsageService;

        public ChatUsageStatsController(
            ILogger<ChatUsageStatsController> logger,
            McpServerDbContext dbContext,
            IChatUsageService chatUsageService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _chatUsageService = chatUsageService;
        }

        // Get overall chat usage statistics
        [HttpGet("stats")]
        public async Task<ActionResult<ChatUsageStatsResponse>> GetChatUsageStats()
        {
            try
            {
                _logger.LogInformation("Getting overall chat usage statistics");
                var stats = await _chatUsageService.GetOverallStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat usage statistics");
                return StatusCode(500, new { error = "An error occurred while retrieving chat usage statistics" });
            }
        }

        // Get usage logs with filtering options
        [HttpGet("logs")]
        public async Task<ActionResult<List<ChatUsageLogResponse>>> GetChatUsageLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? modelId,
            [FromQuery] int? providerId,
            [FromQuery] string? sessionId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting chat usage logs with filters");
                var logs = await _chatUsageService.GetFilteredLogsAsync(
                    startDate, endDate, modelId, providerId, sessionId, page, pageSize);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat usage logs");
                return StatusCode(500, new { error = "An error occurred while retrieving chat usage logs" });
            }
        }

        // Get model-specific usage statistics
        [HttpGet("stats/model/{modelId}")]
        public async Task<ActionResult<ModelUsageStatResponse>> GetModelUsageStats(int modelId)
        {
            try
            {
                _logger.LogInformation("Getting usage statistics for model {ModelId}", modelId);
                var stats = await _chatUsageService.GetModelStatsAsync(modelId);

                if (stats == null)
                {
                    return NotFound($"No usage data found for model with ID {modelId}");
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model usage statistics for model ID {ModelId}", modelId);
                return StatusCode(500, new { error = $"An error occurred while retrieving model usage statistics" });
            }
        }

        // Get provider-specific usage statistics
        [HttpGet("stats/provider/{providerId}")]
        public async Task<ActionResult<ProviderUsageStatResponse>> GetProviderUsageStats(int providerId)
        {
            try
            {
                _logger.LogInformation("Getting usage statistics for provider {ProviderId}", providerId);
                var stats = await _chatUsageService.GetProviderStatsAsync(providerId);

                if (stats == null)
                {
                    return NotFound($"No usage data found for provider with ID {providerId}");
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider usage statistics for provider ID {ProviderId}", providerId);
                return StatusCode(500, new { error = $"An error occurred while retrieving provider usage statistics" });
            }
        }

#if DEBUG
        // Diagnostic endpoint to check if data exists and can be retrieved (Development only)
        [HttpGet("debug/test-logs")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> TestLogsAccess()
        {
            try
            {
                _logger.LogInformation("Testing chat usage logs access");
                var diagnosticInfo = await _chatUsageService.GetDiagnosticInfoAsync();
                return Ok(diagnosticInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing chat usage logs access");
                return StatusCode(500, new { error = "An error occurred while testing chat usage logs" });
            }
        }

        // Generate sample data for testing (Development only)
        [HttpPost("debug/generate-sample-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> GenerateSampleData(int count = 10)
        {
            try
            {
                _logger.LogInformation("Generating {Count} sample chat usage logs", count);
                var result = await _chatUsageService.GenerateSampleDataAsync(count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample chat usage logs");
                return StatusCode(500, new { error = "An error occurred while generating sample logs" });
            }
        }

        // Utility endpoint to fix costs for failed requests (Development only)
        [HttpPost("debug/fix-costs")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> FixCostsForFailedRequests()
        {
            try
            {
                _logger.LogInformation("Running cost correction for failed requests");
                var result = await _chatUsageService.FixCostsForFailedRequestsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing costs for failed requests");
                return StatusCode(500, new { error = "An error occurred while fixing costs for failed requests" });
            }
        }

        // Direct fix for specific issues (Development only)
        [HttpPost("debug/direct-fix")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> DirectFixForErrors()
        {
            try
            {
                _logger.LogInformation("Running direct fix for error responses");

                using var dbContext = _dbContext;

                // Get all logs with error responses
                var logsToFix = await dbContext.ChatUsageLogs
                    .Where(l => l.Response.Contains("Error") || l.Response.Contains("error") || l.OutputTokenCount == 0)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} logs with errors", logsToFix.Count);

                foreach (var log in logsToFix)
                {
                    // Set success to false
                    log.Success = false;

                    // Set costs and tokens to 0
                    log.EstimatedCost = 0;
                    log.InputTokenCount = 0;
                    log.OutputTokenCount = 0;

                    _logger.LogInformation("Fixed log ID {LogId}", log.Id);
                }

                // Save changes
                int updated = await dbContext.SaveChangesAsync();

                return Ok(new {
                    logsFound = logsToFix.Count,
                    logsUpdated = updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct fix");
                return StatusCode(500, new { error = "An error occurred during direct fix" });
            }
        }
#endif
    }
}