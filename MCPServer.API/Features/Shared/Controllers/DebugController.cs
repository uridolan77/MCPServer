using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Models;

namespace MCPServer.API.Features.Shared.Controllers
{
    /// <summary>
    /// Controller for debugging purposes
    /// </summary>
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ApiControllerBase
    {
        public DebugController(ILogger<DebugController> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Tests authentication status
        /// </summary>
        [HttpGet("auth")]
        public ActionResult<ApiResponse<object>> TestAuth()
        {
            // Check if the request has an authenticated user
            if (User.Identity?.IsAuthenticated == true)
            {
                // Return user authentication information
                return SuccessResponse<object>(new
                {
                    isAuthenticated = true,
                    username = User.Identity.Name,
                    claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToArray()
                }, "User is authenticated");
            }
            else
            {
                // User is not authenticated
                return SuccessResponse<object>(new
                {
                    isAuthenticated = false,
                    message = "No authenticated user found"
                }, "User is not authenticated");
            }
        }

        /// <summary>
        /// Tests protected endpoint
        /// </summary>
        [Authorize]
        [HttpGet("protected")]
        public ActionResult<ApiResponse<object>> TestProtected()
        {
            // This endpoint requires authentication
            return SuccessResponse<object>(new
            {
                message = $"Authentication successful for user {User.Identity?.Name}",
                claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToArray()
            }, "Protected endpoint accessed successfully");
        }
    }
}
