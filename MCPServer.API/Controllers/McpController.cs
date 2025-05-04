using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/mcp")]
    [Authorize]
    public class McpController : ControllerBase
    {
        private readonly ILogger<McpController> _logger;
        private readonly ILlmService _llmService;
        private readonly IContextService _contextService;
        private readonly IUserService _userService;

        public McpController(
            ILogger<McpController> logger,
            ILlmService llmService,
            IContextService contextService,
            IUserService userService)
        {
            _logger = logger;
            _llmService = llmService;
            _contextService = contextService;
            _userService = userService;
        }

        [HttpPost("message")]
        public async Task<ActionResult<McpResponse>> SendMessage([FromBody] McpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequest("SessionId is required");
                }

                if (string.IsNullOrEmpty(request.UserInput))
                {
                    return BadRequest("UserInput is required");
                }

                // Get the current user
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "User not authenticated" });
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
                return Ok(new McpResponse
                {
                    SessionId = request.SessionId,
                    Output = response,
                    IsComplete = true,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message request");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpDelete("session/{sessionId}")]
        public async Task<ActionResult> DeleteSession(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("SessionId is required");
                }

                // Get the current user
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Check if the session belongs to the user
                if (!await _userService.IsSessionOwnedByUserAsync(sessionId, username))
                {
                    return Forbid("You don't have permission to delete this session");
                }

                var result = await _contextService.DeleteSessionAsync(sessionId);

                if (result)
                {
                    return Ok(new { message = "Session deleted successfully" });
                }
                else
                {
                    return NotFound(new { message = "Session not found or could not be deleted" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while deleting the session");
            }
        }
    }
}
