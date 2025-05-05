using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Usage.Controllers
{
    /// <summary>
    /// Controller for chat usage statistics
    /// </summary>
    [ApiController]
    [Route("api/chat-usage")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class ChatUsageStatsController : ApiControllerBase
    {
        private readonly IChatUsageService _chatUsageService;

        public ChatUsageStatsController(
            ILogger<ChatUsageStatsController> logger,
            IChatUsageService chatUsageService) : base(logger)
        {
            _chatUsageService = chatUsageService ?? throw new ArgumentNullException(nameof(chatUsageService));
        }

        /// <summary>
        /// Get overall chat usage statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<ChatUsageStatsResponse>>> GetChatUsageStats()
        {
            try
            {
                _logger.LogInformation("Getting overall chat usage statistics");
                var stats = await _chatUsageService.GetOverallStatsAsync();
                return SuccessResponse(stats, "Chat usage statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<ChatUsageStatsResponse>("Error getting chat usage statistics", ex);
            }
        }

        /// <summary>
        /// Get usage logs with filtering options
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<ApiResponse<List<ChatUsageLogResponse>>>> GetChatUsageLogs(
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
                return SuccessResponse(logs, "Chat usage logs retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<ChatUsageLogResponse>>("Error getting chat usage logs", ex);
            }
        }

        /// <summary>
        /// Get model-specific usage statistics
        /// </summary>
        [HttpGet("stats/model/{modelId}")]
        public async Task<ActionResult<ApiResponse<ModelUsageStatResponse>>> GetModelUsageStats(int modelId)
        {
            try
            {
                _logger.LogInformation("Getting usage statistics for model {ModelId}", modelId);
                var stats = await _chatUsageService.GetModelStatsAsync(modelId);

                if (stats == null)
                {
                    return NotFoundResponse<ModelUsageStatResponse>($"No usage data found for model with ID {modelId}");
                }

                return SuccessResponse(stats, $"Usage statistics for model {modelId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<ModelUsageStatResponse>($"Error getting model usage statistics for model ID {modelId}", ex);
            }
        }

        /// <summary>
        /// Get provider-specific usage statistics
        /// </summary>
        [HttpGet("stats/provider/{providerId}")]
        public async Task<ActionResult<ApiResponse<ProviderUsageStatResponse>>> GetProviderUsageStats(int providerId)
        {
            try
            {
                _logger.LogInformation("Getting usage statistics for provider {ProviderId}", providerId);
                var stats = await _chatUsageService.GetProviderStatsAsync(providerId);

                if (stats == null)
                {
                    return NotFoundResponse<ProviderUsageStatResponse>($"No usage data found for provider with ID {providerId}");
                }

                return SuccessResponse(stats, $"Usage statistics for provider {providerId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<ProviderUsageStatResponse>($"Error getting provider usage statistics for provider ID {providerId}", ex);
            }
        }

#if DEBUG
        /// <summary>
        /// Diagnostic endpoint to check if data exists and can be retrieved (Development only)
        /// </summary>
        [HttpGet("debug/test-logs")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<object>>> TestLogsAccess()
        {
            try
            {
                _logger.LogInformation("Testing chat usage logs access");
                var diagnosticInfo = await _chatUsageService.GetDiagnosticInfoAsync();
                return SuccessResponse(diagnosticInfo, "Diagnostic information retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<object>("Error testing chat usage logs access", ex);
            }
        }

        /// <summary>
        /// Generate sample data for testing (Development only)
        /// </summary>
        [HttpPost("debug/generate-sample-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<object>>> GenerateSampleData(int count = 10)
        {
            try
            {
                _logger.LogInformation("Generating {Count} sample chat usage logs", count);
                var result = await _chatUsageService.GenerateSampleDataAsync(count);
                return SuccessResponse(result, $"Generated {count} sample chat usage logs");
            }
            catch (Exception ex)
            {
                return ErrorResponse<object>("Error generating sample chat usage logs", ex);
            }
        }

        /// <summary>
        /// Utility endpoint to fix costs for failed requests (Development only)
        /// </summary>
        [HttpPost("debug/fix-costs")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<object>>> FixCostsForFailedRequests()
        {
            try
            {
                _logger.LogInformation("Running cost correction for failed requests");
                var result = await _chatUsageService.FixCostsForFailedRequestsAsync();
                return SuccessResponse(result, "Cost correction for failed requests completed");
            }
            catch (Exception ex)
            {
                return ErrorResponse<object>("Error fixing costs for failed requests", ex);
            }
        }

        /// <summary>
        /// Direct fix for specific issues (Development only)
        /// </summary>
        [HttpPost("debug/direct-fix")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<object>>> DirectFixForErrors()
        {
            try
            {
                _logger.LogInformation("Running direct fix for error responses");

                // Use the service instead of direct DbContext access
                var result = await _chatUsageService.FixCostsForFailedRequestsAsync();
                
                return SuccessResponse(result, "Direct fix for error responses completed");
            }
            catch (Exception ex)
            {
                return ErrorResponse<object>("Error in direct fix", ex);
            }
        }
#endif
    }
}
