using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;
using MCPServer.API.Features.Llm.Models;

namespace MCPServer.API.Features.Llm.Controllers
{
    /// <summary>
    /// Controller for MCP (Model Control Protocol) operations
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    [Authorize]
    public class McpController : ApiControllerBase
    {
        private readonly ILlmService _llmService;
        private readonly IContextService _contextService;
        private readonly IUserService _userService;

        public McpController(
            ILogger<McpController> logger,
            ILlmService llmService,
            IContextService contextService,
            IUserService userService)
            : base(logger)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Sends a message to the LLM service
        /// </summary>
        [HttpPost("message")]
        public async Task<ActionResult<ApiResponse<Models.McpResponse>>> SendMessage([FromBody] Models.McpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequestResponse<Models.McpResponse>("SessionId is required");
                }

                if (string.IsNullOrEmpty(request.UserInput))
                {
                    return BadRequestResponse<Models.McpResponse>("UserInput is required");
                }

                // Get the current user
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return StatusCode(401, ApiResponse<Models.McpResponse>.ErrorResponse("User not authenticated"));
                }

                // Check if the session belongs to the user
                if (!await _userService.IsSessionOwnedByUserAsync(request.SessionId, username))
                {
                    // If not, associate the session with the user
                    await _userService.AddSessionToUserAsync(request.SessionId, username);
                }

                // Get session context
                var context = await _contextService.GetSessionContextAsync(request.SessionId);

                // Add metadata if provided
                if (request.Metadata != null && request.Metadata.Count > 0)
                {
                    foreach (var item in request.Metadata)
                    {
                        context.Metadata[item.Key] = item.Value;
                    }
                    await _contextService.SaveContextAsync(context);
                }

                // Add user information to metadata
                if (!context.Metadata.ContainsKey("username"))
                {
                    context.Metadata["username"] = username;
                    await _contextService.SaveContextAsync(context);
                }

                // Send message to LLM
                var response = await _llmService.SendWithContextAsync(request.UserInput, context);

                // Update context with the new message and response
                await _contextService.UpdateContextAsync(request.SessionId, request.UserInput, response);

                // Return response
                var mcpResponse = new Models.McpResponse
                {
                    SessionId = request.SessionId,
                    Output = response,
                    IsComplete = true,
                    Timestamp = DateTime.UtcNow
                };

                return SuccessResponse(mcpResponse, "Message processed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<Models.McpResponse>("Error processing message request", ex);
            }
        }

        /// <summary>
        /// Deletes a session
        /// </summary>
        [HttpDelete("session/{sessionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSession(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequestResponse<bool>("SessionId is required");
                }

                // Get the current user
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return StatusCode(401, ApiResponse<bool>.ErrorResponse("User not authenticated"));
                }

                // Check if the session belongs to the user
                if (!await _userService.IsSessionOwnedByUserAsync(sessionId, username))
                {
                    return ForbiddenResponse<bool>("You don't have permission to delete this session");
                }

                var result = await _contextService.DeleteSessionAsync(sessionId);

                if (result)
                {
                    return SuccessResponse(true, "Session deleted successfully");
                }
                else
                {
                    return NotFoundResponse<bool>("Session not found or could not be deleted");
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting session {sessionId}", ex);
            }
        }
    }
}
