using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Features.Shared.Controllers
{
    /// <summary>
    /// Controller for endpoints that don't require authentication
    /// </summary>
    [ApiController]
    [Route("api/noauth")]
    public class NoAuthController : ApiControllerBase
    {
        private readonly ILlmProviderService _llmProviderService;

        public NoAuthController(
            ILogger<NoAuthController> logger,
            ILlmProviderService llmProviderService)
            : base(logger)
        {
            _llmProviderService = llmProviderService ?? throw new ArgumentNullException(nameof(llmProviderService));
        }

        /// <summary>
        /// Gets all LLM providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<ActionResult<ApiResponse<List<LlmProvider>>>> GetAllProviders()
        {
            try
            {
                _logger.LogInformation("NoAuthController: Getting all providers");
                var providers = await _llmProviderService.GetAllProvidersAsync();
                return SuccessResponse(providers, "Providers retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<LlmProvider>>("Error getting all providers", ex);
            }
        }

        /// <summary>
        /// Simple ping endpoint to check if the API is running
        /// </summary>
        [HttpGet("ping")]
        public ActionResult<ApiResponse<object>> Ping()
        {
            _logger.LogInformation("NoAuthController: Ping received");
            return SuccessResponse<object>(new { status = "success", timestamp = DateTime.UtcNow }, "API is running");
        }
    }
}
