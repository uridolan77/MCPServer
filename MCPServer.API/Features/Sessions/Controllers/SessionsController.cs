using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Sessions.Controllers
{
    /// <summary>
    /// Controller for managing chat sessions
    /// </summary>
    [ApiController]
    [Route("api/sessions")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class SessionsController : ApiControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(
            ILogger<SessionsController> logger,
            ISessionService sessionService)
            : base(logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        /// <summary>
        /// Gets all sessions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SessionData>>>> GetAllSessions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting all sessions, page {Page}, pageSize {PageSize}", page, pageSize);
                var sessions = await _sessionService.GetAllSessionsAsync(page, pageSize);
                return SuccessResponse(sessions, "Sessions retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<SessionData>>("Error getting all sessions", ex);
            }
        }

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        [HttpGet("{sessionId}")]
        public async Task<ActionResult<ApiResponse<SessionData>>> GetSessionById(string sessionId)
        {
            try
            {
                _logger.LogInformation("Getting session with ID {SessionId}", sessionId);
                var session = await _sessionService.GetSessionDataAsync(sessionId);

                if (session == null)
                {
                    return NotFoundResponse<SessionData>($"Session with ID {sessionId} not found");
                }

                return SuccessResponse(session, $"Session with ID {sessionId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<SessionData>($"Error getting session with ID {sessionId}", ex);
            }
        }

        /// <summary>
        /// Deletes a session
        /// </summary>
        [HttpDelete("{sessionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSession(string sessionId)
        {
            try
            {
                _logger.LogInformation("Deleting session with ID {SessionId}", sessionId);
                var result = await _sessionService.DeleteSessionAsync(sessionId);

                if (!result)
                {
                    return NotFoundResponse<bool>($"Session with ID {sessionId} not found");
                }

                return SuccessResponse(true, $"Session with ID {sessionId} deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting session with ID {sessionId}", ex);
            }
        }
    }
}
