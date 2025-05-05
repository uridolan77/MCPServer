using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Controllers;
using MCPServer.Core.Features.Usage.Services.Interfaces;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Features.Usage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsageController : ApiControllerBase
    {
        private readonly IChatUsageService _chatUsageService;
        private readonly ILogger<UsageController> _logger;

        public UsageController(IChatUsageService chatUsageService, ILogger<UsageController> logger)
        {
            _chatUsageService = chatUsageService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<ChatUsageStatsResponse>>> GetOverallStats()
        {
            try
            {
                var stats = await _chatUsageService.GetOverallStatsAsync();
                return Success(stats, "Usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics");
                return InternalServerError<ChatUsageStatsResponse>("Error retrieving usage statistics");
            }
        }

        [HttpGet("logs")]
        public async Task<ActionResult<ApiResponse<List<ChatUsageLogResponse>>>> GetFilteredLogs(
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
                var logs = await _chatUsageService.GetFilteredLogsAsync(
                    startDate, endDate, modelId, providerId, sessionId, page, pageSize);
                return Success(logs, "Usage logs retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage logs");
                return InternalServerError<List<ChatUsageLogResponse>>("Error retrieving usage logs");
            }
        }

        [HttpGet("models/{modelId}")]
        public async Task<ActionResult<ApiResponse<ModelUsageStatResponse>>> GetModelStats(int modelId)
        {
            try
            {
                var stats = await _chatUsageService.GetModelStatsAsync(modelId);
                if (stats == null)
                {
                    return NotFound<ModelUsageStatResponse>($"No usage statistics found for model with ID {modelId}");
                }
                return Success(stats, "Model usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving model usage statistics for model ID {ModelId}", modelId);
                return InternalServerError<ModelUsageStatResponse>("Error retrieving model usage statistics");
            }
        }

        [HttpGet("providers/{providerId}")]
        public async Task<ActionResult<ApiResponse<ProviderUsageStatResponse>>> GetProviderStats(int providerId)
        {
            try
            {
                var stats = await _chatUsageService.GetProviderStatsAsync(providerId);
                if (stats == null)
                {
                    return NotFound<ProviderUsageStatResponse>($"No usage statistics found for provider with ID {providerId}");
                }
                return Success(stats, "Provider usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider usage statistics for provider ID {ProviderId}", providerId);
                return InternalServerError<ProviderUsageStatResponse>("Error retrieving provider usage statistics");
            }
        }

#if DEBUG
        [HttpGet("diagnostics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDiagnosticInfo()
        {
            try
            {
                var diagnostics = await _chatUsageService.GetDiagnosticInfoAsync();
                return Success(diagnostics, "Diagnostic information retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving diagnostic information");
                return InternalServerError<object>("Error retrieving diagnostic information");
            }
        }

        [HttpPost("fix-costs")]
        public async Task<ActionResult<ApiResponse<object>>> FixCostsForFailedRequests()
        {
            try
            {
                var result = await _chatUsageService.FixCostsForFailedRequestsAsync();
                return Success(result, "Costs for failed requests fixed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing costs for failed requests");
                return InternalServerError<object>("Error fixing costs for failed requests");
            }
        }

        [HttpPost("generate-sample-data")]
        public async Task<ActionResult<ApiResponse<object>>> GenerateSampleData([FromQuery] int count = 10)
        {
            try
            {
                var result = await _chatUsageService.GenerateSampleDataAsync(count);
                return Success(result, $"Generated {count} sample usage logs successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample usage data");
                return InternalServerError<object>("Error generating sample usage data");
            }
        }
#endif
    }
}
