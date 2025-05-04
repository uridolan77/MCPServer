using System;
using System.Collections.Generic;
using System.Security.Claims;
using MCPServer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    /// <summary>
    /// Base controller class for all API controllers
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected readonly ILogger _logger;

        protected ApiControllerBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current user's ID from claims
        /// </summary>
        protected Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : null;
        }

        /// <summary>
        /// Checks if the current user is in the specified role
        /// </summary>
        protected bool IsUserInRole(string role)
        {
            return User.IsInRole(role);
        }

        /// <summary>
        /// Creates a successful response
        /// </summary>
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Operation completed successfully")
        {
            return Ok(ApiResponse<T>.SuccessResponse(data, message));
        }

        /// <summary>
        /// Creates a not found response
        /// </summary>
        protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string message)
        {
            return NotFound(ApiResponse<T>.ErrorResponse(message));
        }

        /// <summary>
        /// Creates a bad request response
        /// </summary>
        protected ActionResult<ApiResponse<T>> BadRequestResponse<T>(string message, List<string>? errors = null)
        {
            return BadRequest(ApiResponse<T>.ErrorResponse(message, errors));
        }

        /// <summary>
        /// Creates a forbidden response
        /// </summary>
        protected ActionResult<ApiResponse<T>> ForbiddenResponse<T>(string message)
        {
            return StatusCode(403, ApiResponse<T>.ErrorResponse(message));
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        protected ActionResult<ApiResponse<T>> ErrorResponse<T>(string message, Exception ex)
        {
            _logger.LogError(ex, message);
            return StatusCode(500, ApiResponse<T>.ErrorResponse(message, ex));
        }

        /// <summary>
        /// Creates a created response
        /// </summary>
        protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string message, string actionName, object routeValues)
        {
            return CreatedAtAction(actionName, routeValues, ApiResponse<T>.SuccessResponse(data, message));
        }

        /// <summary>
        /// Creates a no content response
        /// </summary>
        protected ActionResult NoContentResponse(string logMessage)
        {
            _logger.LogInformation(logMessage);
            return NoContent();
        }
    }
}
