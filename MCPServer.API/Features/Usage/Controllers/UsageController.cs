using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Shared;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Services.Interfaces;
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

        public UsageController(IChatUsageService chatUsageService, ILogger<UsageController> logger)
            : base(logger)
        {
            _chatUsageService = chatUsageService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<ChatUsageStatsResponse>>> GetOverallStats()
        {
            try
            {
                var stats = await _chatUsageService.GetOverallStatsAsync();
                return SuccessResponse(stats, "Usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics");
                return ErrorResponse<ChatUsageStatsResponse>("Error retrieving usage statistics", ex);
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
                return SuccessResponse(logs, "Usage logs retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage logs");
                return ErrorResponse<List<ChatUsageLogResponse>>("Error retrieving usage logs", ex);
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
                    return NotFoundResponse<ModelUsageStatResponse>($"No usage statistics found for model with ID {modelId}");
                }
                return SuccessResponse(stats, "Model usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving model usage statistics for model ID {ModelId}", modelId);
                return ErrorResponse<ModelUsageStatResponse>("Error retrieving model usage statistics", ex);
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
                    return NotFoundResponse<ProviderUsageStatResponse>($"No usage statistics found for provider with ID {providerId}");
                }
                return SuccessResponse(stats, "Provider usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider usage statistics for provider ID {ProviderId}", providerId);
                return ErrorResponse<ProviderUsageStatResponse>("Error retrieving provider usage statistics", ex);
            }
        }

#if DEBUG
        [HttpGet("diagnostics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDiagnosticInfo()
        {
            try
            {
                var diagnostics = await _chatUsageService.GetDiagnosticInfoAsync();
                return SuccessResponse(diagnostics, "Diagnostic information retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving diagnostic information");
                return ErrorResponse<object>("Error retrieving diagnostic information", ex);
            }
        }

        [HttpPost("fix-costs")]
        public async Task<ActionResult<ApiResponse<object>>> FixCostsForFailedRequests()
        {
            try
            {
                var result = await _chatUsageService.FixCostsForFailedRequestsAsync();
                return SuccessResponse(result, "Costs for failed requests fixed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing costs for failed requests");
                return ErrorResponse<object>("Error fixing costs for failed requests", ex);
            }
        }

        [HttpPost("generate-sample-data")]
        public async Task<ActionResult<ApiResponse<object>>> GenerateSampleData([FromQuery] int count = 10)
        {
            try
            {
                var result = await _chatUsageService.GenerateSampleDataAsync(count);
                return SuccessResponse(result, $"Generated {count} sample usage logs successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample usage data");
                return ErrorResponse<object>("Error generating sample usage data", ex);
            }
        }
#endif
    }
}
