using Microsoft.AspNetCore.Mvc;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;
using MCPServer.API.Features.Llm.Models;

namespace MCPServer.API.Features.Llm.Controllers
{
    /// <summary>
    /// Controller for LLM operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LlmController : ApiControllerBase
    {
        private readonly ILlmService _llmService;

        public LlmController(
            ILlmService llmService,
            ILogger<LlmController> logger) : base(logger)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        }

        /// <summary>
        /// Sends a request to the LLM service
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<LlmResponse>>> SendRequest([FromBody] LlmRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestResponse<LlmResponse>("Request cannot be null");
                }

                var response = await _llmService.SendRequestAsync(request);
                return SuccessResponse(response, "Request processed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<LlmResponse>("Error processing LLM request", ex);
            }
        }

        /// <summary>
        /// Sends a request with context to the LLM service
        /// </summary>
        [HttpPost("context")]
        public async Task<ActionResult<ApiResponse<string>>> SendWithContext([FromBody] ContextRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestResponse<string>("Request cannot be null");
                }

                if (string.IsNullOrEmpty(request.UserInput))
                {
                    return BadRequestResponse<string>("User input is required");
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequestResponse<string>("Session ID is required");
                }

                var context = new SessionContext
                {
                    SessionId = request.SessionId,
                    Messages = request.Messages
                };

                var response = await _llmService.SendWithContextAsync(request.UserInput, context);
                return SuccessResponse(response, "Context request processed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<string>("Error processing context request", ex);
            }
        }
    }
}
